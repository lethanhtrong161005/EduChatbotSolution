namespace Domain.DTOs;

public record EmbeddingResult
{
    public required string Model { get; init; }

    public required List<float[]> Vectors { get; init; }

    public int? TokenCount { get; init; }
}
