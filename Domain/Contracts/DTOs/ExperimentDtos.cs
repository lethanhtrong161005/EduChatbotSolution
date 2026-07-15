namespace Domain.Contracts.DTOs;

public enum ExperimentStatus { Queued = 0, PreparingIndex = 1, Running = 2, Evaluating = 3, Completed = 4, Failed = 5 }
public enum ExperimentQuestionStatus { Pending = 0, Running = 1, Completed = 2, Failed = 3 }

public record CreateExperimentRequest
{
    public required string ExperimentName { get; init; }
    public required int SubjectId { get; init; }
    public required IReadOnlyList<int> TestQuestionIds { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
    public required int TopK { get; init; }
    public required double SimilarityThreshold { get; init; }
    public required int MaxContextChunks { get; init; }
    public required string LlmModel { get; init; }
    public required float ChatTemperature { get; init; }
    public required string JudgeModel { get; init; }
    public string? Notes { get; init; }
}

public record ExperimentIndexPreflightRequest
{
    public required int SubjectId { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
}

public record ExperimentIndexPreflightDto
{
    public required bool IsCompatible { get; init; }
    public required bool RequiresReindex { get; init; }
    public required int AffectedDocumentCount { get; init; }
    public string? BlockingReason { get; init; }
}

public record CreateExperimentResponse
{
    public required Guid ExperimentId { get; init; }
    public required ExperimentStatus Status { get; init; }
    public required DateTime QueuedAt { get; init; }
    public required int QuestionCount { get; init; }
    public required bool RequiresReindex { get; init; }
    public required int AffectedDocumentCount { get; init; }
}

public record TestQuestionOptionDto { public required int TestQuestionId { get; init; } public required string ExternalId { get; init; } public required string Question { get; init; } }

public record ExperimentCreateOptionsDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required SubjectAiConfigurationDto CurrentConfiguration { get; init; }
    public required AiConfigurationOptionsDto AiOptions { get; init; }
    public required IReadOnlyList<TestQuestionOptionDto> TestQuestions { get; init; }
}

public record ExperimentConfigurationSnapshotDto
{
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
    public required int TopK { get; init; }
    public required double SimilarityThreshold { get; init; }
    public required int MaxContextChunks { get; init; }
    public required int MaxHistoryMessages { get; init; }
    public required string LlmModel { get; init; }
    public required float ChatTemperature { get; init; }
    public required string ChatPrompt { get; init; }
    public required string ContextPrompt { get; init; }
    public required string NoContextRetrievedPrompt { get; init; }
    public required float CitationExtractionTemperature { get; init; }
    public required string CitationExtractionPrompt { get; init; }
    public required string JudgeModel { get; init; }
    public required string EvaluatorPromptVersion { get; init; }
}

public record RagasStyleScoresDto
{
    public required double? Faithfulness { get; init; }
    public required double? AnswerRelevancy { get; init; }
    public required double? ContextPrecision { get; init; }
    public required double? ContextRecall { get; init; }
}

public record ExperimentSummaryDto
{
    public required Guid ExperimentId { get; init; }
    public required string ExperimentName { get; init; }
    public required int SubjectId { get; init; }
    public required string SubjectCode { get; init; }
    public required ExperimentStatus Status { get; init; }
    public required string ChunkingStrategy { get; init; }
    public required int ChunkSize { get; init; }
    public required int ChunkOverlap { get; init; }
    public required string EmbeddingModel { get; init; }
    public required string LlmModel { get; init; }
    public required string QuestionSetKey { get; init; }
    public required int IndexedDocumentCount { get; init; }
    public required int AffectedDocumentCount { get; init; }
    public required int CompletedQuestionCount { get; init; }
    public required int TotalQuestionCount { get; init; }
    public required RagasStyleScoresDto AggregateScores { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public string? FailureReason { get; init; }
}

public record ExperimentQuestionResultDto
{
    public required Guid TestResponseId { get; init; }
    public required int TestQuestionId { get; init; }
    public required string ExternalId { get; init; }
    public required string Question { get; init; }
    public required string GroundTruth { get; init; }
    public required ExperimentQuestionStatus Status { get; init; }
    public required string? GeneratedAnswer { get; init; }
    public required IReadOnlyList<string> RetrievedContexts { get; init; }
    public required RagasStyleScoresDto Scores { get; init; }
    public string? Explanation { get; init; }
    public string? FailureReason { get; init; }
    public required int? PromptTokens { get; init; }
    public required int? CompletionTokens { get; init; }
    public required long? RetrievalTimeMs { get; init; }
    public required long? TimeToFirstTokenMs { get; init; }
    public required long? TotalResponseTimeMs { get; init; }
}

public record ExperimentResultDto
{
    public required ExperimentSummaryDto Summary { get; init; }
    public required ExperimentConfigurationSnapshotDto Configuration { get; init; }
    public required IReadOnlyList<ExperimentQuestionResultDto> Questions { get; init; }
}

public record MetricComparisonDto { public required string Metric { get; init; } public required double? LeftScore { get; init; } public required double? RightScore { get; init; } public required double? Delta { get; init; } }
public record QuestionScoreComparisonDto { public required int TestQuestionId { get; init; } public required string ExternalId { get; init; } public required string Question { get; init; } public required RagasStyleScoresDto LeftScores { get; init; } public required RagasStyleScoresDto RightScores { get; init; } }
public record ExperimentComparisonDto { public required ExperimentSummaryDto Left { get; init; } public required ExperimentSummaryDto Right { get; init; } public required IReadOnlyList<MetricComparisonDto> Metrics { get; init; } public required IReadOnlyList<QuestionScoreComparisonDto> Questions { get; init; } }
