using Domain.Entities;

namespace Presentation.DTOs;

public class GenerateChatRequest
{
    public Guid SessionId { get; set; }

    public Guid UserMessageClientId { get; set; }

    public Guid AssistantMessageClientId { get; set; }

    public string Content { get; set; } = string.Empty;
}

public class GenerateChatResponse
{
    public required Guid UserMessageId { get; set; }

    public required Guid UserMessageClientId { get; set; }

    public required string UserMessageContent { get; set; }

    public required DateTime UserMessageSentAt { get; set; }

    public required MessageStatus UserMessageStatus { get; set; }

    public required Guid AssistantMessageId { get; set; }

    public required Guid AssistantMessageClientId { get; set; }

    public required DateTime AssistantMessageSentAt { get; set; }

    public required MessageStatus AssistantMessageStatus { get; set; }
}
