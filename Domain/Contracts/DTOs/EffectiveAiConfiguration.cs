namespace Domain.Contracts.DTOs;

public record EffectiveAiConfiguration
{
    public required string ChunkingStrategy { get; init; }

    public required string EmbeddingModel { get; init; }

    public required int TopK { get; init; }

    public required double SimilarityThreshold { get; init; }

    public required string LlmModel { get; init; }

    public required float ChatTemperature { get; init; }

    public required string ChatPrompt { get; init; }

    public required string ContextPrompt { get; init; }

    public required string NoContextRetrievedPrompt { get; init; }

    public required float TitleTemperature { get; init; }

    public required string TitlePrompt { get; init; }

    public required float CitationExtractionTemperature { get; init; }

    public required string CitationExtractionPrompt { get; init; }

    public required int MaxContextChunks { get; init; }

    public required int MaxHistoryMessages { get; init; }
}
