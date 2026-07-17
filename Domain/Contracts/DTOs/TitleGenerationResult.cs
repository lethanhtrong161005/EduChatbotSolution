namespace Domain.Contracts.DTOs;

public record TitleGenerationResult
{
    public required string Title { get; init; }

    public required TitleGenerationMetrics Metrics { get; init; }
}

public record TitleGenerationMetrics
{
    public required long? PromptTokens { get; init; }

    public required long? CompletionTokens { get; init; }

    public required long ResponseTimeMs { get; init; }
}
