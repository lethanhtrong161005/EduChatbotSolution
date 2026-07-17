using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Utils;

namespace Business.Services.AI;

public sealed class AnswerReconstructionService(IUnitOfWork unitOfWork) : IAnswerReconstructionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ChatAnswerReconstructionDto?> ReconstructChatAssistantAsync(Guid assistantMessageId, Guid ownerUserId, CancellationToken cxlTkn = default)
    {
        if (assistantMessageId == Guid.Empty || ownerUserId == Guid.Empty) throw new BadRequestException("Assistant message and owner IDs are required.");
        var message = await _unitOfWork.AnswerReconstructions.GetChatAssistantAsync(assistantMessageId, cxlTkn);
        if (message == null) return null;
        if (message.ChatSession.UserId != ownerUserId) throw new EntityNotFoundException(assistantMessageId);
        Validate(message.RequestMessages, e => e.MessageOrder, message.ResolvedSubjects, e => e.SubjectOrder, message.RetrievedContexts, e => e.RetrievalRank, message.RetrievedContexts.Where(e => e.WasIncludedInPrompt), e => e.PromptOrder ?? 0);

        return new ChatAnswerReconstructionDto
        {
            ChatSessionId = message.ChatSessionId, AssistantMessageId = message.Id, Completeness = message.ReconstructionCompleteness,
            RequestMessages = [.. message.RequestMessages.OrderBy(e => e.MessageOrder).Select(e => new NormalizedRequestMessageDto { MessageOrder = e.MessageOrder, Role = e.Role, Content = e.Content })],
            ResolvedSubjects = [.. message.ResolvedSubjects.OrderBy(e => e.SubjectOrder).Select(e => new ResolvedSubjectSnapshotDto { SubjectOrder = e.SubjectOrder, SourceSubjectId = e.SourceSubjectId, SubjectCode = e.SubjectCode, SubjectName = e.SubjectName })],
            Retrievals = [.. message.RetrievedContexts.OrderBy(e => e.RetrievalRank).Select(Context)], Settings = message.GenerationSettings == null ? null : Settings(message.GenerationSettings),
            Metrics = message.GenerationMetrics == null ? null : new GenerationMetricsSnapshotDto { RetrievedChunkCount = message.GenerationMetrics.RetrievedChunkCount, ContextChunkCount = message.GenerationMetrics.ContextChunkCount, PromptTokens = message.GenerationMetrics.PromptTokens, CompletionTokens = message.GenerationMetrics.CompletionTokens, RetrievalTimeMs = message.GenerationMetrics.RetrievalTimeMs, TimeToFirstTokenMs = message.GenerationMetrics.TimeToFirstTokenMs, TotalResponseTimeMs = message.GenerationMetrics.TotalResponseTimeMs, TokensPerSecond = message.GenerationMetrics.TokensPerSecond },
            RawAnswer = message.RawContent, FinalAnswer = message.Content,
            Citations = [.. message.Citations.OrderBy(e => e.CitationIndex).Select(e => new CitationSnapshotDto { CitationIndex = e.CitationIndex, RetrievalRank = e.RetrievalSnapshot?.RetrievalRank, SimilarityScore = e.SimilarityScore, LocationInDocument = e.LocationInDocument, DocumentTitle = e.RetrievalSnapshot?.DocumentTitle ?? e.Chunk?.Document?.Title, DocumentFileName = e.RetrievalSnapshot?.DocumentFileName ?? e.Chunk?.Document?.OriginalFileName, SubjectCode = e.RetrievalSnapshot?.SubjectCode ?? e.Chunk?.Document?.Subject?.Code, Occurrences = [.. e.CitationOccurrences.OrderBy(x => x.OccurrenceIndex).Select(x => new CitationOccurrenceSnapshotDto { OccurrenceIndex = x.OccurrenceIndex, SupportingQuote = x.SupportingQuote })] })],
        };
    }

    public async Task<ExperimentAnswerReconstructionDto?> ReconstructExperimentResponseAsync(Guid testResponseId, CancellationToken cxlTkn = default)
    {
        if (testResponseId == Guid.Empty) throw new BadRequestException("Test response ID is required.");
        var response = await _unitOfWork.AnswerReconstructions.GetExperimentResponseAsync(testResponseId, cxlTkn);
        if (response == null) return null;
        Validate(response.RequestMessages, e => e.MessageOrder, Array.Empty<ChatMessageSubjectSnapshot>(), e => e.SubjectOrder, response.RetrievedContexts, e => e.RetrievalRank, response.RetrievedContexts.Where(e => e.WasIncludedInPrompt), e => e.PromptOrder ?? 0);
        var configuration = response.Experiment.ConfigurationSnapshot;
        return new ExperimentAnswerReconstructionDto
        {
            ExperimentId = response.ExperimentId, TestResponseId = response.Id, Completeness = response.ReconstructionCompleteness,
            DatasetName = response.DatasetName, DatasetKey = response.DatasetKey, DatasetVersion = response.DatasetVersion, SourceQuestionId = response.SourceQuestionId, ExternalId = response.QuestionExternalId, Language = response.QuestionLanguage, Difficulty = response.QuestionDifficulty, Question = response.Question, GroundTruth = response.GroundTruth,
            RequestMessages = [.. response.RequestMessages.OrderBy(e => e.MessageOrder).Select(e => new NormalizedRequestMessageDto { MessageOrder = e.MessageOrder, Role = e.Role, Content = e.Content })], Retrievals = [.. response.RetrievedContexts.OrderBy(e => e.RetrievalRank).Select(Context)],
            Settings = Settings(configuration), RawAnswer = response.RawGeneratedAnswer, FinalAnswer = response.GeneratedAnswer,
            Metrics = new GenerationMetricsSnapshotDto { PromptTokens = response.PromptTokens, CompletionTokens = response.CompletionTokens, RetrievalTimeMs = response.RetrievalTimeMs, TimeToFirstTokenMs = response.TimeToFirstTokenMs, TotalResponseTimeMs = response.TotalResponseTimeMs },
            EvaluationAttempts = [.. response.EvaluationAttempts.OrderBy(e => e.AttemptNumber).Select(e => Attempt(e, response.CurrentEvaluationAttemptId))],
        };
    }

    private static RetrievalSnapshotDto Context(ChatMessageContext e) => new() { RetrievalRank = e.RetrievalRank, PromptOrder = e.PromptOrder, WasIncludedInPrompt = e.WasIncludedInPrompt, SourceChunkId = e.SourceChunkId, SourceDocumentId = e.SourceDocumentId, SourceSubjectId = e.SourceSubjectId, ChunkIndex = e.ChunkIndex, ChunkText = e.ChunkText, SimilarityScore = e.SimilarityScore, DocumentTitle = e.DocumentTitle, DocumentFileName = e.DocumentFileName, SubjectCode = e.SubjectCode, SubjectName = e.SubjectName, StartPageNumber = e.StartPageNumber, EndPageNumber = e.EndPageNumber, StartSectionTitle = e.StartSectionTitle, EndSectionTitle = e.EndSectionTitle };
    private static RetrievalSnapshotDto Context(TestResponseContext e) => new() { RetrievalRank = e.RetrievalRank, PromptOrder = e.PromptOrder, WasIncludedInPrompt = e.WasIncludedInPrompt, SourceChunkId = e.SourceChunkId, SourceDocumentId = e.SourceDocumentId, SourceSubjectId = e.SourceSubjectId, ChunkIndex = e.ChunkIndex, ChunkText = e.ChunkText, SimilarityScore = e.SimilarityScore, DocumentTitle = e.DocumentTitle, DocumentFileName = e.DocumentFileName, SubjectCode = e.SubjectCode, SubjectName = e.SubjectName, StartPageNumber = e.StartPageNumber, EndPageNumber = e.EndPageNumber, StartSectionTitle = e.StartSectionTitle, EndSectionTitle = e.EndSectionTitle };
    private static GenerationSettingsSnapshotDto Settings(ChatMessageGenerationSettings e) => new() { EmbeddingProvider = e.EmbeddingProvider, EmbeddingModel = e.EmbeddingModel, TopK = e.TopK, SimilarityThreshold = e.SimilarityThreshold, LlmProvider = e.LlmProvider, LlmModel = e.LlmModel, Temperature = e.Temperature, SystemPrompt = e.SystemPrompt, ContextPrompt = e.ContextPrompt, NoContextRetrievedPrompt = e.NoContextRetrievedPrompt, CitationExtractionTemperature = e.CitationExtractionTemperature, CitationExtractionPrompt = e.CitationExtractionPrompt, MaxContextChunks = e.MaxContextChunks, MaxHistoryMessages = e.MaxHistoryMessages, ReasoningEffort = e.ReasoningEffort, ReasoningOutput = e.ReasoningOutput };
    private static GenerationSettingsSnapshotDto Settings(ExperimentConfigurationSnapshot e) => new() { EmbeddingProvider = e.EmbeddingProvider, EmbeddingModel = e.EmbeddingModel, TopK = e.TopK, SimilarityThreshold = e.SimilarityThreshold, LlmProvider = e.LlmProvider, LlmModel = e.LlmModel, Temperature = e.ChatTemperature, SystemPrompt = e.ChatPrompt, ContextPrompt = e.ContextPrompt, NoContextRetrievedPrompt = e.NoContextRetrievedPrompt, CitationExtractionTemperature = e.CitationExtractionTemperature, CitationExtractionPrompt = e.CitationExtractionPrompt, MaxContextChunks = e.MaxContextChunks, MaxHistoryMessages = e.MaxHistoryMessages, ReasoningEffort = "low", ReasoningOutput = "none" };
    private static EvaluationAttemptSnapshotDto Attempt(TestResponseEvaluationAttempt e, Guid? currentId) => new() { EvaluationAttemptId = e.Id, AttemptNumber = e.AttemptNumber, Status = e.Status, IsCurrent = e.Id == currentId, EvaluatorFamily = e.EvaluatorFamily, ContractVersion = e.ContractVersion, ServiceVersion = e.ServiceVersion, RagasVersion = e.RagasVersion, PromptVersion = e.PromptVersion, Language = e.Language, LlmProvider = e.LlmProvider, LlmModel = e.LlmModel, EmbeddingProvider = e.EmbeddingProvider, EmbeddingModel = e.EmbeddingModel, MetricSetKey = e.MetricSetKey, EvaluatorProfileKey = e.EvaluatorProfileKey, StartedAt = e.StartedAt, CompletedAt = e.CompletedAt, Summary = e.Summary, FailureReason = e.FailureReason, Metrics = [.. e.Metrics.OrderBy(x => x.MetricName).Select(x => new EvaluationMetricSnapshotDto { MetricName = x.MetricName, Status = x.Status, Score = x.Score, Reason = x.Reason, ErrorCode = x.ErrorCode, ErrorMessage = x.ErrorMessage, RetryCount = x.RetryCount, DurationMs = x.DurationMs })] };
    private static void Validate<TRequest, TSubject, TContext>(IEnumerable<TRequest> requests, Func<TRequest, int> requestIndex, IEnumerable<TSubject> subjects, Func<TSubject, int> subjectIndex, IEnumerable<TContext> contexts, Func<TContext, int> retrievalIndex, IEnumerable<TContext> promptContexts, Func<TContext, int> promptIndex)
    {
        if (requests.Any()) SequentialIndexValidator.EnsureExact(requests, requestIndex, "Normalized request messages");
        if (subjects.Any()) SequentialIndexValidator.EnsureExact(subjects, subjectIndex, "Resolved subject snapshots");
        if (contexts.Any()) SequentialIndexValidator.EnsureExact(contexts, retrievalIndex, "Retrieval snapshots");
        if (promptContexts.Any()) SequentialIndexValidator.EnsureExact(promptContexts, promptIndex, "Prompt retrieval snapshots");
    }
}
