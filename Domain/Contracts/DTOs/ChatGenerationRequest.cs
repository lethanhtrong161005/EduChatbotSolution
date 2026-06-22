using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record ChatGenerationRequest
{
    public required string UserMessage { get; init; }

    public required IReadOnlyList<int> AllowedSubjects { get; init; }

    public required IReadOnlyList<ChatHistoryMessage> ChatHistory { get; init; }

    public required ChatGenerationSettings Settings { get; init; }
}

public record ChatHistoryMessage
{
    public required ChatRole ChatRole { get; init; }

    public required string Content { get; init; }
}

public record ChatGenerationSettings
{
    public required string EmbeddingModel { get; init; }

    public required int TopK { get; init; }

    public required double SimilarityThreshold { get; init; }

    public required string LlmModel { get; init; }

    public required float Temperature { get; init; }

    public required string SystemPrompt { get; init; }

    public required string ContextPrompt { get; init; }

    public required string NoContextRetrievedPrompt { get; init; }

    public required float CitationExtractionTemperature { get; init; }

    public required string CitationExtractionPrompt { get; init; }

    public required int MaxContextChunks { get; init; }

    public required int MaxHistoryMessages { get; init; }
}
