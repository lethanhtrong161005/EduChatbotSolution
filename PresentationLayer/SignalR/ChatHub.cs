using Domain.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Presentation.Extensions;

namespace Presentation.SignalR;

public class ChatHub : Hub<IChatClient>
{
    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();
        if (context == null) goto ABORT_CONN;

        try
        {
            var userId = context.User.GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Chat(userId));
            return;
        }
        catch (UserClaimException)
        {
            goto ABORT_CONN;
        }

    ABORT_CONN:
        Context.Abort();
        return;
    }
}

public interface IChatClient
{
    Task ReceiveToken(Guid sessionId, string token);

    Task GenerationCompleted(Guid sessionId, Guid assistantMessageId);

    Task GenerationFailed(Guid sessionId, string error);
}
