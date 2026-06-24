using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeWeb;

public class RealtimeHub : Hub<IRealtimeClient>
{
    public async Task JoinPage(string pageType, string? resourceId = null)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Resource(pageType, resourceId));
    }
}

public interface IRealtimeClient
{
    public Task ResourceChanged(ResourceUpdate resourceUpdate);
}

public class ResourceUpdate
{
    public required string ResourceType { get; set; } = string.Empty;

    public required string Action { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public string?[] AlternateResourceId { get; set; } = [];

    public string? ResourceName { get; set; }
}

public static class ResourceRelations
{
    public static readonly string[] UserGroups =
    [
        HubGroups.Resource("profile"),
        HubGroups.Resource("user-manage"),
        HubGroups.Resource("document-details"),
        HubGroups.Resource("home"),
    ];

    public static readonly string[] MembershipGroups =
    [
        HubGroups.Resource("profile"),
        HubGroups.Resource("subject-manage"),
        HubGroups.Resource("chat"),
        HubGroups.Resource("document-details"),
        HubGroups.Resource("document-edit"),
        HubGroups.Resource("document-library"),
    ];

    public static readonly string[] SubjectGroups =
    [
        HubGroups.Resource("profile"),
        HubGroups.Resource("subject-manage"),
        HubGroups.Resource("chat"),
        HubGroups.Resource("document-details"),
        HubGroups.Resource("document-library"),
    ];

    public static readonly string[] ChapterGroups =
    [
        HubGroups.Resource("subject-manage"),
        HubGroups.Resource("document-details"),
        HubGroups.Resource("document-library"),
    ];

    public static readonly string[] DocumentGroups =
    [
        HubGroups.Resource("profile"),
        HubGroups.Resource("document-details"),
        HubGroups.Resource("document-edit"),
        HubGroups.Resource("document-library"),
    ];

    public static readonly string[] CommentGroups =
    [
        HubGroups.Resource("document-details"),
    ];
}
