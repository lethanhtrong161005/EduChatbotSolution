using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using System.Diagnostics;

namespace Business.Services.AI.Experiments;

public sealed class ExperimentEvaluationService(IUnitOfWork unitOfWork, IPythonRagasClient client) : IExperimentEvaluationService
{
    public const string ContractVersion = "ragas-evaluation-v1";
    public const string EvaluatorFamily = "python-ragas";
    private static readonly TimeSpan MetricTimeout = TimeSpan.FromSeconds(180);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPythonRagasClient _client = client;

    public async Task<TestResponseEvaluationAttempt> AppendInitialEvaluationAsync(Guid testResponseId, CancellationToken cxlTkn = default)
    {
        var response = await _unitOfWork.ExperimentEvaluations.GetInputAsync(testResponseId, cxlTkn) ?? throw new EntityNotFoundException(testResponseId);
        if (response.Status != Domain.Contracts.DTOs.ExperimentQuestionStatus.Completed || string.IsNullOrWhiteSpace(response.GeneratedAnswer)) throw new EntityConflictException("Only a successfully answered experiment response can be evaluated.", nameof(TestResponse.Status));
        if (response.ReconstructionCompleteness != ReconstructionCompleteness.Complete) throw new EntityConflictException("Legacy-incomplete responses cannot be evaluated strictly.", nameof(TestResponse.ReconstructionCompleteness));

        var configuration = response.Experiment.ConfigurationSnapshot;
        var attempt = await _unitOfWork.ExperimentEvaluations.AppendAttemptAsync(testResponseId, new TestResponseEvaluationAttempt
        {
            Status = EvaluationAttemptStatus.Pending,
            EvaluatorFamily = EvaluatorFamily,
            ContractVersion = ContractVersion,
            PromptVersion = configuration.EvaluatorPromptVersion,
            Language = response.QuestionLanguage ?? string.Empty,
            LlmProvider = configuration.EvaluatorLlmProvider,
            LlmModel = configuration.EvaluatorLlmModel,
            EmbeddingProvider = configuration.EvaluatorEmbeddingProvider,
            EmbeddingModel = configuration.EvaluatorEmbeddingModel,
            MetricSetKey = configuration.EvaluatorMetricSetKey,
            EvaluatorProfileKey = ProfileKey(configuration),
        }, cxlTkn);

        try
        {
            var capabilities = await _client.GetCapabilitiesAsync(cxlTkn);
            ValidateCapabilities(capabilities, configuration);
            attempt.ServiceVersion = capabilities.ServiceVersion;
            attempt.RagasVersion = capabilities.RagasVersion;
            attempt.EvaluatorProfileKey = ProfileKey(configuration, capabilities.ServiceVersion, capabilities.RagasVersion);
            attempt.Status = EvaluationAttemptStatus.Running;
            attempt.StartedAt = DateTime.UtcNow;
            foreach (var metricName in RagasMetricName.All) attempt.Metrics.Add(new TestResponseEvaluationMetric { MetricName = metricName });
            await _unitOfWork.SaveAsync(cxlTkn);

            using var concurrency = new SemaphoreSlim(2, 2);
            await Task.WhenAll(attempt.Metrics.Select(metric => EvaluateMetricAsync(metric, response, attempt, concurrency, cxlTkn)));
            var succeeded = attempt.Metrics.Count(e => e.Status == EvaluationMetricStatus.Completed);
            attempt.Status = succeeded == attempt.Metrics.Count ? EvaluationAttemptStatus.Completed : succeeded > 0 ? EvaluationAttemptStatus.PartiallyCompleted : EvaluationAttemptStatus.Failed;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.Summary = $"{succeeded}/{attempt.Metrics.Count} metrics completed.";
            attempt.FailureReason = succeeded == 0 ? "No Ragas metric completed successfully." : null;
            await _unitOfWork.SaveAsync(cxlTkn);
            await _unitOfWork.ExperimentEvaluations.SelectCurrentAsync(testResponseId, attempt.Id, cxlTkn);
            response.CurrentEvaluationAttemptId = attempt.Id;
            response.CurrentEvaluationAttempt = attempt;
            return attempt;
        }
        catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
        {
            foreach (var metric in attempt.Metrics.Where(e => e.Status is EvaluationMetricStatus.Pending or EvaluationMetricStatus.Running))
            {
                metric.Status = EvaluationMetricStatus.Failed;
                metric.ErrorCode = "cancelled";
                metric.ErrorMessage = "Evaluation was cancelled.";
            }
            attempt.Status = EvaluationAttemptStatus.Failed;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.FailureReason = "Evaluation was cancelled.";
            await _unitOfWork.SaveAsync(CancellationToken.None);
            await _unitOfWork.ExperimentEvaluations.SelectCurrentAsync(testResponseId, attempt.Id, CancellationToken.None);
            response.CurrentEvaluationAttemptId = attempt.Id;
            response.CurrentEvaluationAttempt = attempt;
            throw;
        }
        catch (Exception ex)
        {
            attempt.Status = EvaluationAttemptStatus.Failed;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.FailureReason = Error(ex);
            await _unitOfWork.SaveAsync(CancellationToken.None);
            await _unitOfWork.ExperimentEvaluations.SelectCurrentAsync(testResponseId, attempt.Id, CancellationToken.None);
            response.CurrentEvaluationAttemptId = attempt.Id;
            response.CurrentEvaluationAttempt = attempt;
            return attempt;
        }
    }

    private async Task EvaluateMetricAsync(TestResponseEvaluationMetric metric, TestResponse response, TestResponseEvaluationAttempt attempt, SemaphoreSlim concurrency, CancellationToken cxlTkn)
    {
        await concurrency.WaitAsync(cxlTkn);
        try
        {
            metric.Status = EvaluationMetricStatus.Running;
            for (var execution = 0; execution < 2; execution++)
            {
                metric.RetryCount = execution;
                var timer = Stopwatch.StartNew();
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cxlTkn);
                    timeout.CancelAfter(MetricTimeout);
                    var requestId = Guid.NewGuid();
                    var result = await _client.EvaluateAsync(Request(requestId, metric.MetricName, response, attempt), timeout.Token);
                    ValidateResponse(result, requestId, metric.MetricName, attempt);
                    var returned = result.Results.Single();
                    if (!string.Equals(returned.Status, "completed", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(returned.ErrorMessage ?? $"Ragas metric '{metric.MetricName}' failed.");
                    if (!returned.Score.HasValue || !double.IsFinite(returned.Score.Value) || returned.Score.Value is < 0 or > 1) throw new InvalidOperationException($"Ragas metric '{metric.MetricName}' returned an invalid score.");
                    metric.Status = EvaluationMetricStatus.Completed;
                    metric.Score = returned.Score;
                    metric.Reason = returned.Reason;
                    metric.ErrorCode = null;
                    metric.ErrorMessage = null;
                    metric.DurationMs = returned.DurationMs ?? timer.ElapsedMilliseconds;
                    return;
                }
                catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    metric.DurationMs = timer.ElapsedMilliseconds;
                    metric.ErrorCode = ex is OperationCanceledException ? "timeout" : "evaluation_error";
                    metric.ErrorMessage = Error(ex);
                    if (execution == 0) continue;
                    metric.Status = EvaluationMetricStatus.Failed;
                }
            }
        }
        finally { concurrency.Release(); }
    }

    private static PythonRagasEvaluationRequest Request(Guid requestId, string metric, TestResponse response, TestResponseEvaluationAttempt attempt) => new()
    {
        RequestId = requestId,
        ContractVersion = attempt.ContractVersion,
        Language = attempt.Language,
        Question = response.Question!,
        Reference = response.GroundTruth!,
        Response = response.GeneratedAnswer!,
        PromptContexts = [.. response.RetrievedContexts.Where(e => e.WasIncludedInPrompt).OrderBy(e => e.PromptOrder).Select(e => e.ChunkText)],
        RetrievalContexts = [.. response.RetrievedContexts.OrderBy(e => e.RetrievalRank).Select(e => e.ChunkText)],
        Metrics = [metric],
        LlmProvider = attempt.LlmProvider,
        LlmModel = attempt.LlmModel,
        EmbeddingProvider = attempt.EmbeddingProvider,
        EmbeddingModel = attempt.EmbeddingModel,
        PromptVersion = attempt.PromptVersion,
    };

    private static void ValidateCapabilities(PythonRagasCapabilities capabilities, ExperimentConfigurationSnapshot configuration)
    {
        if (capabilities.ContractVersion != ContractVersion) throw new InvalidOperationException($"Unsupported Python Ragas contract '{capabilities.ContractVersion}'.");
        if (capabilities.PromptVersion != configuration.EvaluatorPromptVersion) throw new InvalidOperationException($"Python Ragas prompt version '{capabilities.PromptVersion}' does not match the experiment snapshot.");
        if (!RagasMetricName.All.All(capabilities.Metrics.Contains)) throw new InvalidOperationException("The Python Ragas service does not support the complete experiment metric set.");
        if (!capabilities.LlmOptions.Any(e => e.Provider == configuration.EvaluatorLlmProvider && e.Model == configuration.EvaluatorLlmModel)) throw new InvalidOperationException("The selected evaluator LLM is not supported by the Python Ragas service.");
        if (!capabilities.EmbeddingOptions.Any(e => e.Provider == configuration.EvaluatorEmbeddingProvider && e.Model == configuration.EvaluatorEmbeddingModel)) throw new InvalidOperationException("The selected evaluator embedding model is not supported by the Python Ragas service.");
    }

    private static void ValidateResponse(PythonRagasEvaluationResponse response, Guid requestId, string metric, TestResponseEvaluationAttempt attempt)
    {
        if (response.RequestId != requestId || response.ContractVersion != attempt.ContractVersion || response.PromptVersion != attempt.PromptVersion || response.ServiceVersion != attempt.ServiceVersion || response.RagasVersion != attempt.RagasVersion) throw new InvalidOperationException("The Python Ragas response identity does not match the active evaluation attempt.");
        if (response.Results.Count != 1 || response.Results[0].MetricName != metric) throw new InvalidOperationException($"The Python Ragas response did not contain exactly the requested metric '{metric}'.");
    }

    private static string ProfileKey(ExperimentConfigurationSnapshot configuration, string? serviceVersion = null, string? ragasVersion = null) => $"python-ragas|{ContractVersion}|{(string.IsNullOrWhiteSpace(serviceVersion) ? string.Empty : $"{serviceVersion}|")}{(string.IsNullOrWhiteSpace(ragasVersion) ? string.Empty : $"{ragasVersion}|")}{configuration.EvaluatorPromptVersion}|{configuration.EvaluatorLlmProvider}:{configuration.EvaluatorLlmModel}|{configuration.EvaluatorEmbeddingProvider}:{configuration.EvaluatorEmbeddingModel}|{configuration.EvaluatorMetricSetKey}";
    private static string Error(Exception exception) { var message = exception.GetBaseException().Message; return message.Length <= 2000 ? message : message[..2000]; }
}
