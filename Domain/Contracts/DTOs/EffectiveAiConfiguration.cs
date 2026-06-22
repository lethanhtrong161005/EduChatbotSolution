namespace Domain.Contracts.DTOs;

public record EffectiveAiConfiguration
{
    public required string ChunkingStrategy { get; init; }

    public required string EmbeddingModel { get; init; }

    public required int TopK { get; init; }

    public required double SimilarityThreshold { get; init; }

    public required string LlmModel { get; init; }

    public required double Temperature { get; init; }

    public required string SystemPrompt { get; init; }

    public required int MaxContextChunks { get; init; }

    public required int MaxHistoryMessages { get; init; }
}
