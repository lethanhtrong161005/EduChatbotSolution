namespace Domain.Contracts.DTOs;

public record TitleGenerationResult
{
    public required string Title { get; init; }

    public required TitleGenerationMetrics Metrics { get; init; }
}

public record TitleGenerationMetrics
{
    public required int? PromptTokens { get; init; }

    public required int? CompletionTokens { get; init; }

    public required long ResponseTimeMs { get; init; }
}
