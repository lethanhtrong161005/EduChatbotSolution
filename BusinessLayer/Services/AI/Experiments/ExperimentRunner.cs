using AutoMapper;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Business.Services.AI.Experiments;

public sealed class ExperimentRunner(
    IUnitOfWork unitOfWork,
    IAiConfigurationResolver configurationResolver,
    ISubjectReindexCoordinator reindexCoordinator,
    IChatGenerationService chatGenerationService,
    IRagasStyleEvaluator evaluator,
    IMapper mapper,
    ILogger<ExperimentRunner> logger) : IExperimentRunner
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiConfigurationResolver _configurationResolver = configurationResolver;
    private readonly ISubjectReindexCoordinator _reindexCoordinator = reindexCoordinator;
    private readonly IChatGenerationService _chatGenerationService = chatGenerationService;
    private readonly IRagasStyleEvaluator _evaluator = evaluator;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<ExperimentRunner> _logger = logger;

    public async Task RunAsync(Guid experimentId, CancellationToken cxlTkn = default)
    {
        var experiment = await _unitOfWork.ExperimentRuns.GetForRunAsync(experimentId, cxlTkn) ?? throw new EntityNotFoundException(experimentId);
        if (experiment.Status != ExperimentStatus.Queued) return;

        try
        {
            var inherited = await _configurationResolver.GetAiConfigurationAsync(experiment.SubjectId, cxlTkn);
            var configuration = BuildConfiguration(experiment.ConfigurationSnapshot, inherited);
            var preparation = await _unitOfWork.ExperimentRuns.PrepareAsync(experimentId, BuildSubjectConfiguration(experiment.SubjectId, configuration), cxlTkn);
            if (preparation == null) return;

            if (preparation.RequiresReindex)
            {
                await _reindexCoordinator.RunAsync(experiment.SubjectId, configuration, cxlTkn);
                experiment.IndexedDocumentCount = preparation.AffectedDocumentCount;
                experiment.Status = ExperimentStatus.Running;
                await _unitOfWork.SaveAsync(cxlTkn);
            }

            await GenerateResponsesAsync(experiment, configuration, cxlTkn);

            experiment.Status = ExperimentStatus.Evaluating;
            await _unitOfWork.SaveAsync(cxlTkn);

            await EvaluateResponsesAsync(experiment, cxlTkn);
            CompleteExperiment(experiment);
            await _unitOfWork.SaveAsync(cxlTkn);
        }
        catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
        {
            await FailExperimentAsync(experiment, "The experiment was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            await FailExperimentAsync(experiment, Error(ex));
            throw;
        }
    }

    private async Task GenerateResponsesAsync(Experiment experiment, EffectiveAiConfiguration configuration, CancellationToken cxlTkn)
    {
        foreach (var response in experiment.TestResponses.OrderBy(e => e.TestQuestion.ExternalId))
        {
            cxlTkn.ThrowIfCancellationRequested();
            if (response.Status != ExperimentQuestionStatus.Pending) continue;

            response.Status = ExperimentQuestionStatus.Running;
            response.FailureReason = null;
            await _unitOfWork.SaveAsync(cxlTkn);

            try
            {
                var result = await _chatGenerationService.GenerateAnswerAsync(
                    BuildGenerationRequest(experiment.SubjectId, response.TestQuestion.Question, configuration), static _ => Task.CompletedTask, cxlTkn);
                response.GeneratedAnswer = result.Answer;
                _mapper.Map(result.Metrics, response);

                foreach (var context in result.ChunkRetrievalsInContext.Select((value, index) => (value, index)))
                {
                    response.RetrievedContexts.Add(new TestResponseContext { TestResponseId = response.Id, ContextIndex = context.index, ContextText = context.value.ChunkText });
                }

                await _unitOfWork.SaveAsync(cxlTkn);
            }
            catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                response.Status = ExperimentQuestionStatus.Failed;
                response.FailureReason = Error(ex);
                UpdateProgress(experiment);
                await _unitOfWork.SaveAsync(cxlTkn);
                _logger.LogWarning(ex, "Experiment {ExperimentId} question {QuestionId} generation failed.", experiment.Id, response.TestQuestionId);
            }
        }
    }

    private async Task EvaluateResponsesAsync(Experiment experiment, CancellationToken cxlTkn)
    {
        foreach (var response in experiment.TestResponses.Where(e => e.Status == ExperimentQuestionStatus.Running).OrderBy(e => e.TestQuestion.ExternalId))
        {
            cxlTkn.ThrowIfCancellationRequested();

            try
            {
                var evaluation = await _evaluator.EvaluateAsync(new RagasStyleEvaluationRequest
                {
                    Question = response.TestQuestion.Question,
                    GroundTruth = response.TestQuestion.GroundTruth,
                    GeneratedAnswer = response.GeneratedAnswer ?? string.Empty,
                    RetrievedContexts = [.. response.RetrievedContexts.OrderBy(e => e.ContextIndex).Select(e => e.ContextText)],
                    JudgeModel = experiment.ConfigurationSnapshot.JudgeModel,
                }, cxlTkn);

                _mapper.Map(evaluation, response);
                response.Status = ExperimentQuestionStatus.Completed;
                response.FailureReason = null;
            }
            catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                response.Status = ExperimentQuestionStatus.Failed;
                response.FailureReason = Error(ex);
                _logger.LogWarning(ex, "Experiment {ExperimentId} question {QuestionId} evaluation failed.", experiment.Id, response.TestQuestionId);
            }

            UpdateProgress(experiment);
            await _unitOfWork.SaveAsync(cxlTkn);
        }
    }

    private async Task FailExperimentAsync(Experiment experiment, string failureReason)
    {
        var wasPreparingIndex = experiment.Status == ExperimentStatus.PreparingIndex;

        try
        {
            experiment.Status = ExperimentStatus.Failed;
            experiment.FailureReason = failureReason;
            experiment.CompletedAt = DateTime.UtcNow;

            if (wasPreparingIndex) await _unitOfWork.SubjectIndexes.SetAvailabilityAsync(experiment.SubjectId, SubjectIndexAvailability.Failed, CancellationToken.None);
            await _unitOfWork.SaveAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not persist failure state for experiment {ExperimentId}.", experiment.Id);
        }
    }

    private static void CompleteExperiment(Experiment experiment)
    {
        var completed = experiment.TestResponses.Where(e => e.Status == ExperimentQuestionStatus.Completed).ToArray();

        UpdateProgress(experiment);
        experiment.Faithfulness = Average(completed.Select(e => e.Faithfulness));
        experiment.AnswerRelevancy = Average(completed.Select(e => e.AnswerRelevancy));
        experiment.ContextPrecision = Average(completed.Select(e => e.ContextPrecision));
        experiment.ContextRecall = Average(completed.Select(e => e.ContextRecall));
        experiment.Status = completed.Length == 0 ? ExperimentStatus.Failed : ExperimentStatus.Completed;
        experiment.FailureReason = completed.Length == 0 ? "All experiment questions failed." : null;
        experiment.CompletedAt = DateTime.UtcNow;
    }

    private static void UpdateProgress(Experiment experiment) =>
        experiment.CompletedQuestionCount = experiment.TestResponses.Count(e => e.Status is ExperimentQuestionStatus.Completed or ExperimentQuestionStatus.Failed);

    private static EffectiveAiConfiguration BuildConfiguration(ExperimentConfigurationSnapshot snapshot, EffectiveAiConfiguration inherited) => inherited with
    {
        ChunkingStrategy = snapshot.ChunkingStrategy,
        ChunkSize = snapshot.ChunkSize,
        ChunkOverlap = snapshot.ChunkOverlap,
        EmbeddingModel = snapshot.EmbeddingModel,
        TopK = snapshot.TopK,
        SimilarityThreshold = snapshot.SimilarityThreshold,
        MaxContextChunks = snapshot.MaxContextChunks,
        LlmModel = snapshot.LlmModel,
        ChatTemperature = snapshot.ChatTemperature,
        MaxHistoryMessages = snapshot.MaxHistoryMessages,
        ChatPrompt = snapshot.ChatPrompt,
        ContextPrompt = snapshot.ContextPrompt,
        NoContextRetrievedPrompt = snapshot.NoContextRetrievedPrompt,
        CitationExtractionPrompt = snapshot.CitationExtractionPrompt,
        CitationExtractionTemperature = snapshot.CitationExtractionTemperature,
    };

    private static SubjectAiConfiguration BuildSubjectConfiguration(int subjectId, EffectiveAiConfiguration configuration) => new()
    {
        Id = subjectId,
        ChunkingStrategy = configuration.ChunkingStrategy,
        ChunkSize = configuration.ChunkSize,
        ChunkOverlap = configuration.ChunkOverlap,
        EmbeddingModel = configuration.EmbeddingModel,
        TopK = configuration.TopK,
        SimilarityThreshold = configuration.SimilarityThreshold,
        MaxContextChunks = configuration.MaxContextChunks,
        LlmModel = configuration.LlmModel,
        ChatTemperature = configuration.ChatTemperature,
        MaxHistoryMessages = configuration.MaxHistoryMessages,
        ChatPrompt = configuration.ChatPrompt,
        ContextPrompt = configuration.ContextPrompt,
        NoContextRetrievedPrompt = configuration.NoContextRetrievedPrompt,
        TitleTemperature = configuration.TitleTemperature,
        TitlePrompt = configuration.TitlePrompt,
        CitationExtractionTemperature = configuration.CitationExtractionTemperature,
        CitationExtractionPrompt = configuration.CitationExtractionPrompt,
    };

    private static ChatGenerationRequest BuildGenerationRequest(int subjectId, string question, EffectiveAiConfiguration configuration) => new()
    {
        UserMessage = question,
        AllowedSubjects = [subjectId],
        ChatHistory = [],
        Settings = new ChatGenerationSettings
        {
            EmbeddingModel = configuration.EmbeddingModel,
            TopK = configuration.TopK,
            SimilarityThreshold = configuration.SimilarityThreshold,
            LlmModel = configuration.LlmModel,
            Temperature = configuration.ChatTemperature,
            SystemPrompt = configuration.ChatPrompt,
            ContextPrompt = configuration.ContextPrompt,
            NoContextRetrievedPrompt = configuration.NoContextRetrievedPrompt,
            CitationExtractionTemperature = configuration.CitationExtractionTemperature,
            CitationExtractionPrompt = configuration.CitationExtractionPrompt,
            MaxContextChunks = configuration.MaxContextChunks,
            MaxHistoryMessages = configuration.MaxHistoryMessages,
        },
    };

    private static double? Average(IEnumerable<double?> values)
    {
        var present = values.Where(e => e.HasValue).Select(e => e!.Value).ToArray();
        return present.Length == 0 ? null : present.Average();
    }

    private static string Error(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        return message.Length <= 2000 ? message : message[..2000];
    }
}
