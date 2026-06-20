namespace Domain.Contracts.DTOs;

public record ChunkResult
{
    public required int ChunkIndex { get; init; }

    public required string ChunkText { get; init; }

    public int? PageNumber { get; init; }

    public string? SectionTitle { get; init; }
}
