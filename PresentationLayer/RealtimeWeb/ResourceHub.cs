using Microsoft.AspNetCore.SignalR;

namespace Presentation.RealtimeWeb;

public class ResourceHub : Hub<IResourceClient>
{
    public async Task JoinGroup(string pageType, string? resourceId)
    {
        // Join fallback group.
        // This group is used when exact entity IDs are too costly to retrieve.
        // e.g.,
        // Update subject name
        // -> Every document details page of a document in that subject must update
        // -> However, subject.Chapters.Documents requires a DB query
        // -> Cheaper to just send to all document details page and let client filter
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Resource(pageType, null));

        if (resourceId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Resource(pageType, resourceId));
    }
}

public interface IResourceClient
{
    public Task ResourceChanged(ResourceUpdate resourceUpdate);
}

public record ResourceUpdate
{
    public required string ResourceType { get; init; } = string.Empty;

    public required string Action { get; init; } = string.Empty;

    public required string ResourceId { get; init; }

    public string? ResourceName { get; init; }

    public Dictionary<string, object> Properties { get; init; } = [];
}

public static class ResourceTypes
{
    public const string User = "user";
    public const string Membership = "membership";
    public const string Subject = "subject";
    public const string Chapter = "chapter";
    public const string Document = "document";
    public const string Comment = "comment";

    // ChatMessages are managed by AiChatHub
    public const string ChatSession = "chat-session";
}

public static class Actions
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";

    public const string Disabled = "disabled";
    public const string Reactivated = "reactivated";
}

public static class PageTypes
{
    public const string Home = "home";
    public const string Profile = "profile";
    public const string UserManage = "user-manage";
    public const string SubjectManage = "subject-manage";
    public const string DocumentLibrary = "document-library";
    public const string DocumentDetails = "document-details";
    public const string DocumentEdit = "document-edit";
    public const string Chat = "chat";
}
