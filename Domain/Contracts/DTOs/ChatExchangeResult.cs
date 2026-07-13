namespace Domain.Contracts.DTOs;

public record ChatExchangeResult
{
    public required ResolvedChatMessage UserMessage { get; init; }
    public required ResolvedChatMessage AssistantMessage { get; init; }
}
