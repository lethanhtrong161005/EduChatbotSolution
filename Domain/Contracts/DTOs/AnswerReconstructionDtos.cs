using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record NormalizedRequestMessageDto { public required int MessageOrder { get; init; } public required string Role { get; init; } public required string Content { get; init; } }
public record ResolvedSubjectSnapshotDto { public required int SubjectOrder { get; init; } public required int SourceSubjectId { get; init; } public required string SubjectCode { get; init; } public required string SubjectName { get; init; } }
public record RetrievalSnapshotDto
{
    public required int RetrievalRank { get; init; } public int? PromptOrder { get; init; } public required bool WasIncludedInPrompt { get; init; }
    public Guid? SourceChunkId { get; init; } public Guid? SourceDocumentId { get; init; } public int? SourceSubjectId { get; init; } public int? ChunkIndex { get; init; }
    public required string ChunkText { get; init; } public double? SimilarityScore { get; init; } public string? DocumentTitle { get; init; } public string? DocumentFileName { get; init; }
    public string? SubjectCode { get; init; } public string? SubjectName { get; init; } public int? StartPageNumber { get; init; } public int? EndPageNumber { get; init; }
    public string? StartSectionTitle { get; init; } public string? EndSectionTitle { get; init; }
}
public record GenerationSettingsSnapshotDto
{
    public required string EmbeddingProvider { get; init; } public required string EmbeddingModel { get; init; } public required int TopK { get; init; } public required double SimilarityThreshold { get; init; }
    public required string LlmProvider { get; init; } public required string LlmModel { get; init; } public required float Temperature { get; init; } public required string SystemPrompt { get; init; }
    public required string ContextPrompt { get; init; } public required string NoContextRetrievedPrompt { get; init; } public required float CitationExtractionTemperature { get; init; }
    public required string CitationExtractionPrompt { get; init; } public required int MaxContextChunks { get; init; } public required int MaxHistoryMessages { get; init; }
    public required string ReasoningEffort { get; init; } public required string ReasoningOutput { get; init; }
}
public record GenerationMetricsSnapshotDto
{
    public int? RetrievedChunkCount { get; init; } public int? ContextChunkCount { get; init; } public long? PromptTokens { get; init; } public long? CompletionTokens { get; init; }
    public long? RetrievalTimeMs { get; init; } public long? TimeToFirstTokenMs { get; init; } public long? TotalResponseTimeMs { get; init; } public double? TokensPerSecond { get; init; }
}
public record CitationOccurrenceSnapshotDto { public required int OccurrenceIndex { get; init; } public required string SupportingQuote { get; init; } }
public record CitationSnapshotDto
{
    public required int CitationIndex { get; init; } public int? RetrievalRank { get; init; } public required double SimilarityScore { get; init; } public string? LocationInDocument { get; init; }
    public string? DocumentTitle { get; init; } public string? DocumentFileName { get; init; } public string? SubjectCode { get; init; } public required IReadOnlyList<CitationOccurrenceSnapshotDto> Occurrences { get; init; }
}
public record EvaluationMetricSnapshotDto { public required string MetricName { get; init; } public required EvaluationMetricStatus Status { get; init; } public double? Score { get; init; } public string? Reason { get; init; } public string? ErrorCode { get; init; } public string? ErrorMessage { get; init; } public required int RetryCount { get; init; } public long? DurationMs { get; init; } }
public record EvaluationAttemptSnapshotDto
{
    public required Guid EvaluationAttemptId { get; init; } public required int AttemptNumber { get; init; } public required EvaluationAttemptStatus Status { get; init; } public required bool IsCurrent { get; init; }
    public required string EvaluatorFamily { get; init; } public required string ContractVersion { get; init; } public required string ServiceVersion { get; init; } public required string RagasVersion { get; init; }
    public required string PromptVersion { get; init; } public required string Language { get; init; } public required string LlmProvider { get; init; } public required string LlmModel { get; init; }
    public required string EmbeddingProvider { get; init; } public required string EmbeddingModel { get; init; } public required string MetricSetKey { get; init; } public required string EvaluatorProfileKey { get; init; }
    public DateTime? StartedAt { get; init; } public DateTime? CompletedAt { get; init; } public string? Summary { get; init; } public string? FailureReason { get; init; }
    public required IReadOnlyList<EvaluationMetricSnapshotDto> Metrics { get; init; }
}
public record ChatAnswerReconstructionDto
{
    public required Guid ChatSessionId { get; init; } public required Guid AssistantMessageId { get; init; } public required ReconstructionCompleteness Completeness { get; init; }
    public required IReadOnlyList<NormalizedRequestMessageDto> RequestMessages { get; init; } public required IReadOnlyList<ResolvedSubjectSnapshotDto> ResolvedSubjects { get; init; }
    public required IReadOnlyList<RetrievalSnapshotDto> Retrievals { get; init; } public GenerationSettingsSnapshotDto? Settings { get; init; } public GenerationMetricsSnapshotDto? Metrics { get; init; }
    public required string RawAnswer { get; init; } public required string FinalAnswer { get; init; } public required IReadOnlyList<CitationSnapshotDto> Citations { get; init; }
}
public record ExperimentAnswerReconstructionDto
{
    public required Guid ExperimentId { get; init; } public required Guid TestResponseId { get; init; } public required ReconstructionCompleteness Completeness { get; init; }
    public string? DatasetName { get; init; } public string? DatasetKey { get; init; } public string? DatasetVersion { get; init; } public int? SourceQuestionId { get; init; }
    public string? ExternalId { get; init; } public string? Language { get; init; } public string? Difficulty { get; init; } public string? Question { get; init; } public string? GroundTruth { get; init; }
    public required IReadOnlyList<NormalizedRequestMessageDto> RequestMessages { get; init; } public required IReadOnlyList<RetrievalSnapshotDto> Retrievals { get; init; }
    public GenerationSettingsSnapshotDto? Settings { get; init; } public required string? RawAnswer { get; init; } public required string? FinalAnswer { get; init; } public required GenerationMetricsSnapshotDto Metrics { get; init; }
    public required IReadOnlyList<EvaluationAttemptSnapshotDto> EvaluationAttempts { get; init; }
}
