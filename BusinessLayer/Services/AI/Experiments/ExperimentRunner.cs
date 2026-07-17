using AutoMapper;
using DataAccess.UnitOfWork;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Utils;
using Microsoft.Extensions.Logging;

namespace Business.Services.AI.Experiments;

public sealed class ExperimentRunner(
    IUnitOfWork unitOfWork,
    IAiConfigurationResolver configurationResolver,
    ISubjectReindexCoordinator reindexCoordinator,
    IChatGenerationService chatGenerationService,
    IExperimentEvaluationService evaluationService,
    IMapper mapper,
    ILogger<ExperimentRunner> logger) : IExperimentRunner
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiConfigurationResolver _configurationResolver = configurationResolver;
    private readonly ISubjectReindexCoordinator _reindexCoordinator = reindexCoordinator;
    private readonly IChatGenerationService _chatGenerationService = chatGenerationService;
    private readonly IExperimentEvaluationService _evaluationService = evaluationService;
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
        foreach (var response in experiment.TestResponses.OrderBy(e => e.QuestionExternalId))
        {
            cxlTkn.ThrowIfCancellationRequested();
            if (response.Status != ExperimentQuestionStatus.Pending) continue;

            response.Status = ExperimentQuestionStatus.Running;
            response.FailureReason = null;
            await _unitOfWork.SaveAsync(cxlTkn);

            try
            {
                var result = await _chatGenerationService.GenerateAnswerAsync(
                    BuildGenerationRequest(experiment.SubjectId, response.Question ?? throw new InvalidOperationException("The response has no immutable question snapshot."), configuration), static _ => Task.CompletedTask, cxlTkn);
                SequentialIndexValidator.EnsureExact(result.RetrievedContexts, e => e.RetrievalRank, "Retrieved experiment contexts");
                SequentialIndexValidator.EnsureExact(result.RetrievedContexts.Where(e => e.WasIncludedInPrompt), e => e.PromptOrder ?? 0, "Prompt experiment contexts");
                SequentialIndexValidator.EnsureExact(result.RequestMessages, e => e.MessageOrder, "Normalized experiment request messages");
                if (result.RetrievedContexts.Any(e => e.WasIncludedInPrompt != e.PromptOrder.HasValue)) throw new InvalidOperationException("Retrieved experiment prompt membership and prompt order disagree.");
                response.GeneratedAnswer = result.Answer;
                response.RawGeneratedAnswer = result.RawAnswer;
                _mapper.Map(result.Metrics, response);

                foreach (var context in result.RetrievedContexts)
                {
                    response.RetrievedContexts.Add(new TestResponseContext
                    {
                        TestResponseId = response.Id,
                        RetrievalRank = context.RetrievalRank,
                        PromptOrder = context.PromptOrder,
                        WasIncludedInPrompt = context.WasIncludedInPrompt,
                        ChunkId = context.ChunkId,
                        SourceChunkId = context.ChunkId,
                        SourceDocumentId = context.SourceDocumentId,
                        SourceSubjectId = context.SourceSubjectId,
                        ChunkIndex = context.ChunkIndex,
                        ChunkText = context.ChunkText,
                        SimilarityScore = context.SimilarityScore,
                        DocumentTitle = context.DocumentTitle,
                        DocumentFileName = context.DocumentFileName,
                        SubjectCode = context.SubjectCode,
                        SubjectName = context.SubjectName,
                        StartPageNumber = context.StartPageNumber,
                        EndPageNumber = context.EndPageNumber,
                        StartSectionTitle = context.StartSectionTitle,
                        EndSectionTitle = context.EndSectionTitle,
                    });
                }

                foreach (var requestMessage in result.RequestMessages.OrderBy(e => e.MessageOrder)) response.RequestMessages.Add(new TestResponseRequestMessage { MessageOrder = requestMessage.MessageOrder, Role = requestMessage.Role, Content = requestMessage.Content });
                response.ReconstructionCompleteness = ReconstructionCompleteness.Complete;
                response.Status = ExperimentQuestionStatus.Completed;
                UpdateProgress(experiment);

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
        foreach (var response in experiment.TestResponses.Where(e => e.Status == ExperimentQuestionStatus.Completed && e.CurrentEvaluationAttemptId == null).OrderBy(e => e.QuestionExternalId))
        {
            cxlTkn.ThrowIfCancellationRequested();

            try
            {
                await _evaluationService.AppendInitialEvaluationAsync(response.Id, cxlTkn);
            }
            catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
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
        var usable = experiment.TestResponses.Count(e => e.CurrentEvaluationAttempt?.Metrics.Any(metric => metric.Status == EvaluationMetricStatus.Completed) == true);

        UpdateProgress(experiment);
        experiment.Status = usable == 0 ? ExperimentStatus.Failed : ExperimentStatus.Completed;
        experiment.FailureReason = usable == 0 ? "No experiment question obtained a usable evaluation result." : null;
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
            EmbeddingProvider = AiProviderName.ForEmbeddingModel(configuration.EmbeddingModel),
            EmbeddingModel = configuration.EmbeddingModel,
            TopK = configuration.TopK,
            SimilarityThreshold = configuration.SimilarityThreshold,
            LlmProvider = AiProviderName.ForChatModel(configuration.LlmModel),
            LlmModel = configuration.LlmModel,
            Temperature = configuration.ChatTemperature,
            SystemPrompt = configuration.ChatPrompt,
            ContextPrompt = configuration.ContextPrompt,
            NoContextRetrievedPrompt = configuration.NoContextRetrievedPrompt,
            CitationExtractionTemperature = configuration.CitationExtractionTemperature,
            CitationExtractionPrompt = configuration.CitationExtractionPrompt,
            MaxContextChunks = configuration.MaxContextChunks,
            MaxHistoryMessages = configuration.MaxHistoryMessages,
            ReasoningEffort = "low",
            ReasoningOutput = "none",
        },
    };

    private static string Error(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        return message.Length <= 2000 ? message : message[..2000];
    }
}
