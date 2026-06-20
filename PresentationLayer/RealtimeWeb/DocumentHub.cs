using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeWeb;

public class DocumentHub : Hub<IDocumentClient>
{
    private const string QueryParamName_PageType = "page";
    private const string QueryParamName_DocumentId = "documentId";

    private const string PageType_DocumentLibrary = "library";
    private const string PageType_DocumentDetails = "details";

    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();
        if (context == null) goto ABORT_CONN;

        var pageType = context.Request.Query[QueryParamName_PageType].ToString();
        if (string.IsNullOrWhiteSpace(pageType)) goto ABORT_CONN;

        switch (pageType)
        {
            case PageType_DocumentLibrary:
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.DocumentLibrary);
                return;
            case PageType_DocumentDetails:
                var documentIdStr = context.Request.Query[QueryParamName_DocumentId].ToString();
                if (string.IsNullOrWhiteSpace(documentIdStr)) goto ABORT_CONN;
                if (!Guid.TryParse(documentIdStr, out var docId)) goto ABORT_CONN;
                await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.DocumentDetails(docId));
                return;
            default:
                goto ABORT_CONN;
        }

    ABORT_CONN:
        Context.Abort();
        return;
    }
}

public interface IDocumentClient
{
    Task UpdateStatus(DocumentStatusUpdate documentStatusUpdate);
}
