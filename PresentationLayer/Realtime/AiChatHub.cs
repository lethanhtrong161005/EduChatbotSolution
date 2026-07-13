using Microsoft.AspNetCore.SignalR;
using Presentation.DTOs;

namespace Presentation.Realtime;

public class AiChatHub : Hub<IAiChatClient>
{
    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Chat(sessionId));
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Chat(sessionId));
    }

    public async Task SwitchSession(Guid oldSessionId, Guid newSessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Chat(oldSessionId));
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Chat(newSessionId));
    }
}

public interface IAiChatClient
{
    Task ExchangeCreated(Guid userMessageId, Guid userMessageClientId, Guid assistantMessageId, Guid assistantMessageClientId);

    Task GenerationStarted(Guid assistantMessageId, Guid assistantMessageClientId);

    Task ReceiveToken(Guid assistantMessageId, Guid assistantMessageClientId, string token);

    Task GenerationCompleted(Guid assistantMessageId, Guid assistantMessageClientId, ChatMessageDto chatMessageDto);

    Task GenerationFailed(Guid assistantMessageId, Guid assistantMessageClientId, string error);

    Task AssistantVariantCreated(Guid userMessageId, Guid assistantMessageId, Guid assistantMessageClientId, ChatVariantNavigationDto variantNavigation);

    Task AssistantVariantSelected(Guid userMessageId, Guid assistantMessageId, ChatVariantNavigationDto variantNavigation);
}
