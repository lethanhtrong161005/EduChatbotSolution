using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Realtime;

public class SignalRResourceRealtimeNotifier(
    IHubContext<ResourceHub, IResourceClient> hub)
    : IResourceRealtimeNotifier
{
    private readonly IHubContext<ResourceHub, IResourceClient> _hub = hub;

    public async Task PushUpdateAsync(ResourceUpdate update, string? callerConnectionId = null)
    {
        // 1. Calculate cascading event chain (e.g., change principal in a 1:1 relationship -> eject old dependent entity)

        // 2. Generate JSON payload for cascaded events.

        // 3. Generate monotonic correlation ID. Apply to all payloads.

        // 4. Broadcast each event.

        await BroadcastAsync(update, callerConnectionId);
    }

    private async Task BroadcastAsync(ResourceUpdate update, string? callerConnectionId = null)
    {
        var groups = GetGroups(update);

        var tasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(callerConnectionId))
        {
            foreach (var group in groups)
            {
                tasks.Add(_hub.Clients.GroupExcept(group, callerConnectionId).ResourceChanged(update));
            }
        }
        else
        {
            foreach (var group in groups)
            {
                tasks.Add(_hub.Clients.Group(group).ResourceChanged(update));
            }
        }

        await Task.WhenAll(tasks);
    }

    private static List<string> GetGroups(ResourceUpdate update)
    {
        var resourceType = update.ResourceType.ToString().ToKebabCaseLower();

        List<string> groups = [HubGroups.Resource(resourceType)];

        if (update.Action != ResourceAction.Created)
            groups.Add(HubGroups.Resource(resourceType, update.ResourceId));

        try
        {
            groups.AddRange(GetCollectionGroups(update));
        }
        catch
        {
            // Log and move on
        }

        return groups;
    }

    private static IEnumerable<string> GetCollectionGroups(ResourceUpdate update)
    {
        // 1. Switch by resouce type

        // 2. Check for FK changes

        // 3. For each FK change: Route to old and new collection groups

        switch (update.ResourceType)
        {
            case ResourceType.User:
                break;
            case ResourceType.Membership:
                if (update.Properties.TryGetValue(nameof(ChatSession.SubjectId), out object? value) && value is string subjectId_Membership)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.Subject).ToKebabCaseLower(), subjectId_Membership.ToString(), nameof(ResourceType.Membership).ToKebabCaseLower());
                if (update.Properties.TryGetValue(nameof(ChatSession.UserId), out value) && value is string userId_Membership)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.User).ToKebabCaseLower(), userId_Membership.ToString(), nameof(ResourceType.Membership).ToKebabCaseLower());
                break;
            case ResourceType.Subject:
                break;
            case ResourceType.Chapter:
                if (update.Properties.TryGetValue(nameof(Chapter.SubjectId), out value) && value is string subjectId_Chapter)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.Subject).ToKebabCaseLower(), subjectId_Chapter.ToString(), nameof(ResourceType.Chapter).ToKebabCaseLower());
                break;
            case ResourceType.Document:
                if (update.Properties.TryGetValue(nameof(Document.SubjectId), out value) && value is string subjectId_Document)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.Subject).ToKebabCaseLower(), subjectId_Document.ToString(), nameof(ResourceType.Document).ToKebabCaseLower());
                if (update.Properties.TryGetValue(nameof(Document.UploaderId), out value) && value is string uploaderId_Document)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.User).ToKebabCaseLower(), uploaderId_Document.ToString(), nameof(ResourceType.Document).ToKebabCaseLower());
                break;
            case ResourceType.ChatSession:
                if (update.Properties.TryGetValue(nameof(ChatSession.UserId), out value) && value is string userId_ChatSession)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.User).ToKebabCaseLower(), userId_ChatSession.ToString(), nameof(ResourceType.ChatSession).ToKebabCaseLower());
                if (update.Properties.TryGetValue(nameof(ChatSession.SubjectId), out value) && value is string subjectId_ChatSession)
                    yield return HubGroups.ResourceCollection(nameof(ResourceType.Subject).ToKebabCaseLower(), subjectId_ChatSession.ToString(), nameof(ResourceType.ChatSession).ToKebabCaseLower());
                break;
        }
    }
}
