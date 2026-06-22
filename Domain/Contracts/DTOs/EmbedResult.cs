namespace Domain.Contracts.DTOs;

public record EmbedResult
{
    public required string Model { get; init; }

    public required IReadOnlyList<ReadOnlyMemory<float>> Vectors { get; init; }

    public long? InputTokenCount { get; init; }

    public long? OutputTokenCount { get; init; }
}
