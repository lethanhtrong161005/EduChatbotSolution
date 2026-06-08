using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeNotif;

public class DocumentHub : Hub<IDocumentClient>
{
    private const string QueryParamName_PageType = "page";
    private const string QueryParamName_DocumentId = "documentId";

    private const string PageType_DocumentLibrary = "library";
    private const string PageType_DocumentDetails = "details";

    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();
        if (context == null) return;

        var pageType = context.Request.Query[QueryParamName_PageType].ToString();
        if (string.IsNullOrWhiteSpace(pageType)) return;

        switch (pageType)
        {
            case PageType_DocumentLibrary:
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.DocumentLibrary);
                break;
            case PageType_DocumentDetails:
                var docId = context.Request.Query[QueryParamName_DocumentId].ToString();
                if (string.IsNullOrWhiteSpace(docId)) return;
                if (!Guid.TryParse(docId, out var id)) return;
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.DocumentDetails(id));
                break;
        }
    }
}
