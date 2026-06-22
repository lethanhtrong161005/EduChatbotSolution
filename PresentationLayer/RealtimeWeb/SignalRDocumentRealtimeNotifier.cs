using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeWeb;

public class SignalRDocumentRealtimeNotifier(
    IHubContext<DocumentHub, IDocumentClient> documentHub)
    : IDocumentRealtimeNotifier
{
    private readonly IHubContext<DocumentHub, IDocumentClient> _docHub = documentHub;

    public async Task UpdateStatus(DocumentStatusUpdate docStatusUpd)
    {
        var libGroup = HubGroups.DocumentLibrary;
        var detailsGroup = HubGroups.DocumentDetails(docStatusUpd.Id);
        await _docHub.Clients.Groups([libGroup, detailsGroup]).UpdateStatus(docStatusUpd);
    }
}
