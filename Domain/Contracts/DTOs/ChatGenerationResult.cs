namespace Domain.Contracts.DTOs;

public record ChatGenerationResult
{
    public required string Answer { get; init; }

    public required string RawAnswer { get; init; }

    public required IReadOnlyList<ChunkRetrieval> ChunkRetrievals { get; init; }

    public required IReadOnlyList<ChunkRetrieval> ChunkRetrievalsInContext { get; init; }

    public required IReadOnlyList<ChunkUsage> ChunkUsages { get; init; }

    public required ChatGenerationMetrics Metrics { get; init; }
}

public record ChunkRetrieval
{
    public required Guid ChunkId { get; init; }

    public required string ChunkText { get; init; }

    public required double SimilarityScore { get; init; }
}

public record ChunkUsage
{
    public required Guid ChunkId { get; init; }

    public required int CitationIndex { get; init; }

    public required double SimilarityScore { get; init; }
}

public record ChatGenerationMetrics
{
    public required int PromptTokens { get; init; }

    public required int CompletionTokens { get; init; }

    public required long RetrievalTimeMs { get; init; }

    public required long TimeToFirstTokenMs { get; init; }

    public required long TotalResponseTimeMs { get; init; }

    public required double TokensPerSecond { get; init; }
}
