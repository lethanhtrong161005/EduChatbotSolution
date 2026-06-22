namespace Domain.Contracts.DTOs;

public record ResolvedCitation
{
    public required Guid ChunkId { get; init; }

    public required int OccurrenceIndex { get; init; }

    public required int CitationIndex { get; init; }

    public string? QuotedText { get; init; }

    public required double SimilarityScore { get; init; }

    public required int ChunkIndex { get; init; }

    public required string ChunkText { get; init; }

    public required string? LocationInDocument { get; init; }

    public required string DocumentTitle { get; init; }

    public required Guid DocumentId { get; init; }
}
