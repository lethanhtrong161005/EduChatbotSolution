namespace Domain.Contracts.DTOs;

public record TitleGenerationRequest
{
    public required string UserMessage { get; init; }

    public required string? AssistantMessage { get; init; }

    public required ParsedAttachment[] Attachments { get; init; }

    public required TitleGenerationSettings Settings { get; init; }
}

public record ParsedAttachment
{
    public required string Name { get; set; }

    public required string TextContent { get; set; }
}

public record TitleGenerationSettings
{
    public required string LlmModel { get; init; }

    public required float Temperature { get; init; }

    public required string SystemPrompt { get; init; }
}
