using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Realtime;

public class SignalRDocumentStatusRealtimeNotifier(
    IHubContext<DocumentStatusHub, IDocumentClient> documentHub)
    : IDocumentStatusRealtimeNotifier
{
    private readonly IHubContext<DocumentStatusHub, IDocumentClient> _docHub = documentHub;

    public async Task PushUpdateAsync(DocumentStatusUpdate docStatusUpd)
    {
        var libGroup = HubGroups.DocumentStatus();
        var detailsGroup = HubGroups.DocumentStatus(docStatusUpd.Id);
        await _docHub.Clients.Groups([libGroup, detailsGroup]).UpdateStatus(docStatusUpd);
    }
}
