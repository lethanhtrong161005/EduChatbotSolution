namespace Domain.Contracts.DTOs;

public record ChunkResult
{
    public required int ChunkIndex { get; init; }

    public required string ChunkText { get; init; }

    public int? StartPageNumber { get; init; }

    public int? EndPageNumber { get; init; }

    public string? StartSectionTitle { get; init; }

    public string? EndSectionTitle { get; init; }
}
