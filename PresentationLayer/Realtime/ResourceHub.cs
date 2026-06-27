using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Realtime;

public class ResourceHub : Hub<IResourceClient>
{
    public async Task SubscribeToResourceType(string resourceType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Resource(resourceType));
    }

    public async Task SubscribeToResource(string resourceType, string resourceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Resource(resourceType, resourceId));
    }

    public async Task SubscribeToResourceCollection(string principalType, string principalId, string dependentType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ResourceCollections(principalType, principalId, dependentType));
    }

    public async Task UnsubscribeFromResourceType(string resourceType)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Resource(resourceType));
    }

    public async Task UnsubscribeFromResource(string resourceType, string resourceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Resource(resourceType, resourceId));
    }

    public async Task UnsubscribeFromResourceCollection(string principalType, string principalId, string dependentType)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.ResourceCollections(principalType, principalId, dependentType));
    }
}

public interface IResourceClient
{
    public Task ResourceChanged(ResourceUpdate resourceUpdate);
}
