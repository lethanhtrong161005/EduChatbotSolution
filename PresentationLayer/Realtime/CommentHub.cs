using Microsoft.AspNetCore.SignalR;

namespace Presentation.Realtime;

public class CommentHub : Hub<ICommentClient>
{
    public async Task JoinDocumentCommentSection(string documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.CommentSection(documentId));
    }
}

public interface ICommentClient
{

}
