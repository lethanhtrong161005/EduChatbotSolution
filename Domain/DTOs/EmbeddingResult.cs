namespace Domain.DTOs;

public record EmbeddingResult
{
    public required string Model { get; init; }

    public required IReadOnlyList<float[]> Vectors { get; init; }

    public int? TokenCount { get; init; }
}
