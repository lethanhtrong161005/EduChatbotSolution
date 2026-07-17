namespace Domain.Contracts.DTOs;

public record ChatGenerationResult
{
    public required string Answer { get; init; }

    public required string RawAnswer { get; init; }

    public required IReadOnlyList<ChunkRetrieval> ChunkRetrievals { get; init; }

    public required IReadOnlyList<RetrievedContextSnapshot> RetrievedContexts { get; init; }

    public IReadOnlyList<NormalizedRequestMessageSnapshot> RequestMessages { get; init; } = [];

    public IReadOnlyList<ResolvedSubjectSnapshot> ResolvedSubjects { get; init; } = [];

    public required IReadOnlyList<ChunkUsage> ChunkUsages { get; init; }

    public required ChatGenerationMetrics Metrics { get; init; }
}

public record RetrievedContextSnapshot
{
    public required int RetrievalRank { get; init; }
    public required int? PromptOrder { get; init; }
    public required bool WasIncludedInPrompt { get; init; }
    public required Guid ChunkId { get; init; }
    public Guid SourceDocumentId { get; init; }
    public int SourceSubjectId { get; init; }
    public int ChunkIndex { get; init; }
    public required string ChunkText { get; init; }
    public double SimilarityScore { get; init; }
    public string DocumentTitle { get; init; } = string.Empty;
    public string DocumentFileName { get; init; } = string.Empty;
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public int? StartPageNumber { get; init; }
    public int? EndPageNumber { get; init; }
    public string? StartSectionTitle { get; init; }
    public string? EndSectionTitle { get; init; }
    public string ChunkingStrategy { get; init; } = string.Empty;
    public int ChunkSize { get; init; }
    public int ChunkOverlap { get; init; }
    public string? EmbeddingModel { get; init; }
}

public record NormalizedRequestMessageSnapshot { public required int MessageOrder { get; init; } public required string Role { get; init; } public required string Content { get; init; } }

public record ResolvedSubjectSnapshot { public required int SubjectOrder { get; init; } public required int SubjectId { get; init; } public required string SubjectCode { get; init; } public required string SubjectName { get; init; } }

public record ChunkRetrieval
{
    public required Guid ChunkId { get; init; }
    public Guid DocumentId { get; init; }
    public int SubjectId { get; init; }
    public int ChunkIndex { get; init; }
    public required string ChunkText { get; init; }
    public required double SimilarityScore { get; init; }
    public string DocumentTitle { get; init; } = string.Empty;
    public string DocumentFileName { get; init; } = string.Empty;
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectName { get; init; } = string.Empty;
    public int? StartPageNumber { get; init; }
    public int? EndPageNumber { get; init; }
    public string? StartSectionTitle { get; init; }
    public string? EndSectionTitle { get; init; }
    public string ChunkingStrategy { get; init; } = string.Empty;
    public int ChunkSize { get; init; }
    public int ChunkOverlap { get; init; }
    public string? EmbeddingModel { get; init; }
}

public record ChunkUsage
{
    public required Guid ChunkId { get; init; }

    public required int CitationIndex { get; init; }

    public required double SimilarityScore { get; init; }
}

public record ChatGenerationMetrics
{
    public required long? PromptTokens { get; init; }

    public required long? CompletionTokens { get; init; }

    public required long RetrievalTimeMs { get; init; }

    public required long? TimeToFirstTokenMs { get; init; }

    public required long TotalResponseTimeMs { get; init; }

    public required double? TokensPerSecond { get; init; }
}
