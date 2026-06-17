using Domain.Contracts;
using Domain.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeNotif;

public class SignalRDocumentRealtimeNotifier(
    IHubContext<DocumentHub, IDocumentClient> documentHub)
    : IDocumentRealtimeNotifier
{
    private readonly IHubContext<DocumentHub, IDocumentClient> _docHub = documentHub;

    public async Task UpdateStatus(DocumentStatusUpdate docStatusUpd)
    {
        var libGrp = HubGroups.DocumentLibrary;
        var detailsGrp = HubGroups.DocumentDetails(docStatusUpd.Id);
        await _docHub.Clients.Groups([libGrp, detailsGrp]).UpdateStatus(docStatusUpd);
    }
}
