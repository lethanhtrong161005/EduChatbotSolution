using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeWeb;

public class SignalRDocumentStatusRealtimeNotifier(
    IHubContext<DocumentStatusHub, IDocumentClient> documentHub)
    : IDocumentStatusRealtimeNotifier
{
    private readonly IHubContext<DocumentStatusHub, IDocumentClient> _docHub = documentHub;

    public async Task UpdateStatus(DocumentStatusUpdate docStatusUpd)
    {
        var libGroup = HubGroups.DocumentStatusLibrary();
        var detailsGroup = HubGroups.DocumentStatusDetails(docStatusUpd.Id);
        await _docHub.Clients.Groups([libGroup, detailsGroup]).UpdateStatus(docStatusUpd);
    }
}
