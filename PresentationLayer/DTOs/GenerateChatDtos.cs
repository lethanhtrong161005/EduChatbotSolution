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
    public Guid UserMessageId { get; set; }

    public Guid AssistantMessageId { get; set; }

    public Guid UserMessageClientId { get; set; }

    public Guid AssistantMessageClientId { get; set; }
}
