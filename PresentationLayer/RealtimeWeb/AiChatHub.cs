using Microsoft.AspNetCore.SignalR;
using Presentation.DTOs;

namespace Presentation.RealtimeWeb;

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
}

public interface IAiChatClient
{
    Task ReceiveToken(Guid assistantMessageId, string token);

    Task GenerationCompleted(Guid assistantMessageId, ChatMessageDto chatMessageDto);

    Task GenerationFailed(Guid assistantMessageId, string error);
}
