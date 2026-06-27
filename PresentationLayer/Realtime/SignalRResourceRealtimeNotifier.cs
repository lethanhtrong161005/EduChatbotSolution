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
        var resourceType = update.ResourceType.ToString().ToKebabCaseLower();

        string[] groups =
        [
            HubGroups.Resource(resourceType),
            HubGroups.Resource(resourceType, update.ResourceId),
            .. GetCollectionGroups(update),
        ];

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

    private static IEnumerable<string> GetCollectionGroups(ResourceUpdate update)
    {
        // Fail-fast if caller did not set properties properly
        switch (update.ResourceType)
        {
            case ResourceType.User:
                break;
            case ResourceType.Membership:
                yield return HubGroups.ResourceCollections(nameof(ResourceType.Subject).ToKebabCaseLower(), update.Properties[nameof(SubjectMembership.SubjectId)]!.ToString()!, nameof(ResourceType.Membership).ToKebabCaseLower());
                yield return HubGroups.ResourceCollections(nameof(ResourceType.User).ToKebabCaseLower(), update.Properties[nameof(SubjectMembership.UserId)]!.ToString()!, nameof(ResourceType.Membership).ToKebabCaseLower());
                break;
            case ResourceType.Subject:
                break;
            case ResourceType.Chapter:
                yield return HubGroups.ResourceCollections(nameof(ResourceType.Subject).ToKebabCaseLower(), update.Properties[nameof(Chapter.SubjectId)]!.ToString()!, nameof(ResourceType.Chapter).ToKebabCaseLower());
                break;
            case ResourceType.Document:
                yield return HubGroups.ResourceCollections(nameof(ResourceType.Subject).ToKebabCaseLower(), update.Properties[nameof(Document.Chapter.SubjectId)]!.ToString()!, nameof(ResourceType.Document).ToKebabCaseLower());
                yield return HubGroups.ResourceCollections(nameof(ResourceType.Chapter).ToKebabCaseLower(), update.Properties[nameof(Document.ChapterId)]!.ToString()!, nameof(ResourceType.Document).ToKebabCaseLower());
                yield return HubGroups.ResourceCollections(nameof(ResourceType.User).ToKebabCaseLower(), update.Properties[nameof(Document.UploaderId)]!.ToString()!, nameof(ResourceType.Document).ToKebabCaseLower());
                break;
            case ResourceType.ChatSession:
                yield return HubGroups.ResourceCollections(nameof(ResourceType.User).ToKebabCaseLower(), update.Properties[nameof(ChatSession.UserId)]!.ToString()!, nameof(ResourceType.ChatSession).ToKebabCaseLower());
                if (update.Properties.TryGetValue(nameof(ChatSession.SubjectId), out object? value) && value != null)
                    yield return HubGroups.ResourceCollections(nameof(ResourceType.Subject).ToKebabCaseLower(), value.ToString()!, nameof(ResourceType.Document).ToKebabCaseLower());
                break;
        }
    }
}
