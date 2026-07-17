using Business.Services.AI.Experiments;
using DataAccess.Data;
using DataAccess.Repositories;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests;

[TestFixture]
public sealed class ExperimentEvaluationServiceTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ExperimentEvaluationRepository> _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _repository = new Mock<ExperimentEvaluationRepository>(new EduChatAiDbContext(new DbContextOptionsBuilder<EduChatAiDbContext>().UseNpgsql("Host=localhost;Database=experiment_evaluation_model_only;Username=unused;Password=unused", npgsql => npgsql.UseVector()).Options));
        _unitOfWork.SetupGet(e => e.ExperimentEvaluations).Returns(_repository.Object);
        _unitOfWork.Setup(e => e.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task AppendInitialEvaluationAsync_AllMetricsSucceed_PersistsOneAttemptAndLimitsConcurrencyToTwo()
    {
        var response = Response();
        ConfigureRepository(response);
        var active = 0;
        var maximum = 0;
        var requests = new List<PythonRagasEvaluationRequest>();
        var client = new StubPythonRagasClient(Capabilities(), async request =>
        {
            lock (requests) requests.Add(request);
            var current = Interlocked.Increment(ref active);
            SetMaximum(ref maximum, current);
            await Task.Delay(25);
            Interlocked.Decrement(ref active);
            return Success(request, .8);
        });

        var attempt = await new ExperimentEvaluationService(_unitOfWork.Object, client).AppendInitialEvaluationAsync(response.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempt.AttemptNumber, Is.EqualTo(1));
            Assert.That(attempt.Status, Is.EqualTo(EvaluationAttemptStatus.Completed));
            Assert.That(attempt.Metrics.Select(e => e.MetricName), Is.EquivalentTo(RagasMetricName.All));
            Assert.That(attempt.Metrics.All(e => e.Status == EvaluationMetricStatus.Completed && e.Score == .8 && e.RetryCount == 0), Is.True);
            Assert.That(attempt.EvaluatorProfileKey, Is.EqualTo("python-ragas|ragas-evaluation-v1|1.0.0|0.4.3|vi-ragas-v1|gemini:gemini/gemini-2.5-flash|gemini:gemini/gemini-embedding-001|ragas-rag-core-v1"));
            Assert.That(response.CurrentEvaluationAttemptId, Is.EqualTo(attempt.Id));
            Assert.That(maximum, Is.LessThanOrEqualTo(2));
            Assert.That(requests.All(e => e.PromptContexts.SequenceEqual(["Prompt context"]) && e.RetrievalContexts.SequenceEqual(["Prompt context", "Retrieved only"])), Is.True);
        }

        _repository.Verify(e => e.SelectCurrentAsync(response.Id, attempt.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AppendInitialEvaluationAsync_FirstMetricCallFails_RetriesOnlyThatMetricWithinSameAttempt()
    {
        var response = Response();
        ConfigureRepository(response);
        var calls = new Dictionary<string, int>(StringComparer.Ordinal);
        var client = new StubPythonRagasClient(Capabilities(), request =>
        {
            calls[request.Metrics.Single()] = calls.GetValueOrDefault(request.Metrics.Single()) + 1;
            if (request.Metrics.Single() == RagasMetricName.Faithfulness && calls[request.Metrics.Single()] == 1) throw new HttpRequestException("Transient failure");
            return Task.FromResult(Success(request, .7));
        });

        var attempt = await new ExperimentEvaluationService(_unitOfWork.Object, client).AppendInitialEvaluationAsync(response.Id);
        var faithfulness = attempt.Metrics.Single(e => e.MetricName == RagasMetricName.Faithfulness);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempt.Status, Is.EqualTo(EvaluationAttemptStatus.Completed));
            Assert.That(faithfulness.Status, Is.EqualTo(EvaluationMetricStatus.Completed));
            Assert.That(faithfulness.RetryCount, Is.EqualTo(1));
            Assert.That(calls[RagasMetricName.Faithfulness], Is.EqualTo(2));
            Assert.That(calls.Where(e => e.Key != RagasMetricName.Faithfulness).All(e => e.Value == 1), Is.True);
        }
    }

    [Test]
    public async Task AppendInitialEvaluationAsync_OneMetricExhaustsRetry_SelectsPartialAttemptAndPreservesSuccessfulMetrics()
    {
        var response = Response();
        ConfigureRepository(response);
        var client = new StubPythonRagasClient(Capabilities(), request => request.Metrics.Single() == RagasMetricName.ContextRecall ? throw new InvalidOperationException("Metric failed") : Task.FromResult(Success(request, .6)));

        var attempt = await new ExperimentEvaluationService(_unitOfWork.Object, client).AppendInitialEvaluationAsync(response.Id);
        var failed = attempt.Metrics.Single(e => e.MetricName == RagasMetricName.ContextRecall);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempt.Status, Is.EqualTo(EvaluationAttemptStatus.PartiallyCompleted));
            Assert.That(attempt.Metrics.Count(e => e.Status == EvaluationMetricStatus.Completed), Is.EqualTo(3));
            Assert.That(failed.Status, Is.EqualTo(EvaluationMetricStatus.Failed));
            Assert.That(failed.RetryCount, Is.EqualTo(1));
            Assert.That(failed.ErrorMessage, Does.Contain("Metric failed"));
            Assert.That(response.CurrentEvaluationAttempt, Is.SameAs(attempt));
        }
    }

    [Test]
    public async Task AppendInitialEvaluationAsync_CapabilityLookupFails_SelectsFailedAttemptWithoutMetrics()
    {
        var response = Response();
        ConfigureRepository(response);
        var client = new StubPythonRagasClient(new HttpRequestException("Ragas unavailable"));

        var attempt = await new ExperimentEvaluationService(_unitOfWork.Object, client).AppendInitialEvaluationAsync(response.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempt.Status, Is.EqualTo(EvaluationAttemptStatus.Failed));
            Assert.That(attempt.Metrics, Is.Empty);
            Assert.That(attempt.FailureReason, Does.Contain("Ragas unavailable"));
            Assert.That(response.CurrentEvaluationAttemptId, Is.EqualTo(attempt.Id));
        }
    }

    [Test]
    public void AppendInitialEvaluationAsync_CancelledEvaluation_SelectsFailedTerminalAttemptAndPropagatesCancellation()
    {
        var response = Response();
        ConfigureRepository(response);
        using var cxlTknSource = new CancellationTokenSource();
        var client = new StubPythonRagasClient(Capabilities(), async _ => { await cxlTknSource.CancelAsync(); await Task.Delay(Timeout.Infinite, cxlTknSource.Token); throw new AssertionException("Cancellation must interrupt evaluation."); });

        Assert.That(async () => await new ExperimentEvaluationService(_unitOfWork.Object, client).AppendInitialEvaluationAsync(response.Id, cxlTknSource.Token), Throws.InstanceOf<OperationCanceledException>());

        var attempt = response.EvaluationAttempts.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempt.Status, Is.EqualTo(EvaluationAttemptStatus.Failed));
            Assert.That(attempt.FailureReason, Is.EqualTo("Evaluation was cancelled."));
            Assert.That(attempt.CompletedAt, Is.Not.Null);
            Assert.That(attempt.Metrics.All(e => e.Status == EvaluationMetricStatus.Failed && e.ErrorCode == "cancelled"), Is.True);
            Assert.That(response.CurrentEvaluationAttemptId, Is.EqualTo(attempt.Id));
            Assert.That(response.CurrentEvaluationAttempt, Is.SameAs(attempt));
        }
        _repository.Verify(e => e.SelectCurrentAsync(response.Id, attempt.Id, CancellationToken.None), Times.Once);
    }

    private void ConfigureRepository(TestResponse response)
    {
        _repository.Setup(e => e.GetInputAsync(response.Id, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        _repository.Setup(e => e.AppendAttemptAsync(response.Id, It.IsAny<TestResponseEvaluationAttempt>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, TestResponseEvaluationAttempt attempt, CancellationToken _) =>
        {
            attempt.Id = Guid.NewGuid();
            attempt.AttemptNumber = response.EvaluationAttempts.Count + 1;
            response.EvaluationAttempts.Add(attempt);
            return attempt;
        });
        _repository.Setup(e => e.SelectCurrentAsync(response.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private static TestResponse Response()
    {
        var experiment = new Experiment
        {
            Id = Guid.NewGuid(),
            ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            ConfigurationSnapshot = new ExperimentConfigurationSnapshot
            {
                EvaluatorLlmProvider = "gemini", EvaluatorLlmModel = "gemini/gemini-2.5-flash", EvaluatorEmbeddingProvider = "gemini", EvaluatorEmbeddingModel = "gemini/gemini-embedding-001",
                EvaluatorMetricSetKey = "ragas-rag-core-v1", EvaluatorPromptVersion = "vi-ragas-v1",
            },
        };
        var response = new TestResponse
        {
            Id = Guid.NewGuid(), ExperimentId = experiment.Id, Experiment = experiment, Status = ExperimentQuestionStatus.Completed, ReconstructionCompleteness = ReconstructionCompleteness.Complete,
            QuestionLanguage = "vi", Question = "Câu hỏi?", GroundTruth = "Câu trả lời tham chiếu.", GeneratedAnswer = "Câu trả lời được sinh.",
        };
        response.RetrievedContexts.Add(new TestResponseContext { RetrievalRank = 1, PromptOrder = 1, WasIncludedInPrompt = true, ChunkText = "Prompt context" });
        response.RetrievedContexts.Add(new TestResponseContext { RetrievalRank = 2, WasIncludedInPrompt = false, ChunkText = "Retrieved only" });
        return response;
    }

    private static PythonRagasCapabilities Capabilities() => new()
    {
        ContractVersion = ExperimentEvaluationService.ContractVersion, ServiceVersion = "1.0.0", RagasVersion = "0.4.3", PromptVersion = "vi-ragas-v1", Metrics = RagasMetricName.All,
        LlmOptions = [new PythonRagasModelOption { Provider = "gemini", Model = "gemini/gemini-2.5-flash", Label = "Gemini 2.5 Flash" }],
        EmbeddingOptions = [new PythonRagasModelOption { Provider = "gemini", Model = "gemini/gemini-embedding-001", Label = "Gemini Embedding" }],
    };

    private static PythonRagasEvaluationResponse Success(PythonRagasEvaluationRequest request, double score) => new()
    {
        RequestId = request.RequestId, ContractVersion = request.ContractVersion, ServiceVersion = "1.0.0", RagasVersion = "0.4.3", PromptVersion = request.PromptVersion,
        Results = [new PythonRagasMetricResult { MetricName = request.Metrics.Single(), Status = "completed", Score = score, Reason = "Grounded", DurationMs = 12 }],
    };

    private static void SetMaximum(ref int target, int value)
    {
        int current;
        do { current = target; if (value <= current) return; } while (Interlocked.CompareExchange(ref target, value, current) != current);
    }

    private sealed class StubPythonRagasClient : IPythonRagasClient
    {
        private readonly PythonRagasCapabilities? _capabilities;
        private readonly Exception? _capabilityFailure;
        private readonly Func<PythonRagasEvaluationRequest, Task<PythonRagasEvaluationResponse>> _evaluate;

        public StubPythonRagasClient(PythonRagasCapabilities capabilities, Func<PythonRagasEvaluationRequest, Task<PythonRagasEvaluationResponse>> evaluate) { _capabilities = capabilities; _evaluate = evaluate; }
        public StubPythonRagasClient(Exception capabilityFailure) { _capabilityFailure = capabilityFailure; _evaluate = _ => throw new AssertionException("Evaluation must not start when capabilities are unavailable."); }
        public Task<PythonRagasHealth> GetHealthAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PythonRagasCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default) => _capabilityFailure is null ? Task.FromResult(_capabilities!) : Task.FromException<PythonRagasCapabilities>(_capabilityFailure);
        public Task<PythonRagasEvaluationResponse> EvaluateAsync(PythonRagasEvaluationRequest request, CancellationToken cancellationToken = default) => _evaluate(request);
    }
}
