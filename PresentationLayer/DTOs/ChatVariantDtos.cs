using Domain.Entities;

namespace Presentation.DTOs;

public class ChatVariantOptionDto
{
    public Guid MessageId { get; set; }
    public int VariantIndex { get; set; }
    public bool IsSelected { get; set; }
    public MessageStatus Status { get; set; }
}

public class ChatVariantNavigationDto
{
    public Guid UserMessageId { get; set; }
    public int CurrentVariantIndex { get; set; }
    public int TotalVariantCount { get; set; }
    public List<ChatVariantOptionDto> Variants { get; set; } = [];
}

public class RetryAssistantMessageRequest { public Guid MessageId { get; set; } public Guid AssistantMessageClientId { get; set; } }
public class RegenerateAssistantMessageRequest { public Guid MessageId { get; set; } public Guid AssistantMessageClientId { get; set; } }
public class SelectAssistantVariantRequest { public Guid MessageId { get; set; } }

public class StartAssistantGenerationResponse
{
    public Guid AssistantMessageId { get; set; }
    public Guid AssistantMessageClientId { get; set; }
    public MessageStatus Status { get; set; }
    public ChatVariantNavigationDto VariantNavigation { get; set; } = null!;
}

public class DeleteChatSessionResponse { public Guid SessionId { get; set; } }
