using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using NuGet.Packaging.Signing;

namespace Presentation.RealtimeWeb;

public class ResourceRealtimeNotifier(
    IHubContext<ResourceHub, IResourceClient> hub)
    : IResourceRealtimeNotifier
{
    private readonly IHubContext<ResourceHub, IResourceClient> _hub = hub;

    private async Task BroadcastAsync(
        ICollection<string> targetGroups,
        ICollection<string> originGroups,
        string? callerConnectionId,
        ResourceUpdate update)
    {
        if (originGroups.Count == 0
            || string.IsNullOrWhiteSpace(callerConnectionId))
        {
            await _hub.Clients.Groups(targetGroups).ResourceChanged(update);
            return;
        }

        var tasks = new List<Task>();

        foreach (var group in targetGroups)
        {
            if (originGroups.Contains(group))
                tasks.Add(_hub.Clients.GroupExcept(group, callerConnectionId).ResourceChanged(update));
            else
                tasks.Add(_hub.Clients.Group(group).ResourceChanged(update));
        }

        await Task.WhenAll(tasks);
    }

    #region User

    private const string PropKey_UploadedDocumentsIds = "uploadedDocumentsIds";

    private async Task<Document[]> GetDocumentsUploadedByUser(ApplicationUser user)
    {
        return [];
    }

    private async Task UserChanged(
        string action,
        ApplicationUser user,
        string? originPageType,
        string? callerConnectionId)
    {
        var docIds = (await GetDocumentsUploadedByUser(user)).Select(e => e.Id).ToArray();

        var targetGroups = NotificationTargets.User(user.Id, docIds);

        var originGroups = !string.IsNullOrWhiteSpace(originPageType)
            ? [HubGroups.Resource(originPageType, user.Id.ToString()), HubGroups.Resource(originPageType)]
            : Array.Empty<string>();

        var update = new ResourceUpdate
        {
            ResourceType = ResourceTypes.User,
            Action = action,
            ResourceId = user.Id.ToString(),
            ResourceName = user.FullName,
            Properties =
            {
                { PropKey_UploadedDocumentsIds, docIds },
            },
        };

        await BroadcastAsync(targetGroups, originGroups, callerConnectionId, update);
    }

    public async Task UserCreated(ApplicationUser user, string? originPageType = null, string? callerConnectionId = null) =>
      await UserChanged(Actions.Created, user, originPageType, callerConnectionId);

    public async Task UserUpdated(ApplicationUser user, string? originPageType = null, string? callerConnectionId = null) =>
      await UserChanged(Actions.Updated, user, originPageType, callerConnectionId);

    public async Task UserDeleted(ApplicationUser user, string? originPageType = null, string? callerConnectionId = null) =>
        await UserChanged(Actions.Deleted, user, originPageType, callerConnectionId);

    public async Task UserDisabled(ApplicationUser user, string? originPageType = null, string? callerConnectionId = null) =>
        await UserChanged(Actions.Disabled, user, originPageType, callerConnectionId);

    public async Task UserReactiviated(ApplicationUser user, string? originPageType = null, string? callerConnectionId = null) =>
        await UserChanged(Actions.Reactivated, user, originPageType, callerConnectionId);

    #endregion

    #region Subject

    private async Task<IEnumerable<ApplicationUser>> GetUsersAssginedToSubject(Subject subject)
    {
        return [];
    }

    private async Task<IEnumerable<Document>> GetDocumentsInSubject(Subject subject)
    {
        return [];
    }

    private async Task SubjectChanged(
        string action,
        Subject subject,
        string? originPageType,
        string? callerConnectionId)
    {
        var usersTask = GetUsersAssginedToSubject(subject);
        var docsTask = GetDocumentsInSubject(subject);

        var relTasks = new List<Task> { usersTask, docsTask };

        await Task.WhenAll(relTasks);

        var userIds = (await usersTask).Select(e => e.Id).ToArray();
        var docIds = (await docsTask).Select(e => e.Id).ToArray();

        var targetGroups = NotificationTargets.Subject(subject.Id, userIds, docIds);

        var originGroups = !string.IsNullOrWhiteSpace(originPageType)
            ? [HubGroups.Resource(originPageType, subject.Id.ToString()), HubGroups.Resource(originPageType)]
            : Array.Empty<string>();

        var update = new ResourceUpdate
        {
            ResourceType = ResourceTypes.Subject,
            Action = action,
            ResourceId = subject.Id.ToString(),
            ResourceName = subject.Name,
        };

        await BroadcastAsync(targetGroups, originGroup, callerConnectionId, update);
    }

    public async Task SubjectCreated(Subject subject, string? originPageType = null, string? callerConnectionId = null) =>
        await SubjectChanged(Actions.Created, subject, originPageType, callerConnectionId);

    public async Task SubjectUpdated(Subject subject, string? originPageType = null, string? callerConnectionId = null) =>
        await SubjectChanged(Actions.Updated, subject, originPageType, callerConnectionId);

    public async Task SubjectDeleted(Subject subject, string? originPageType = null, string? callerConnectionId = null) =>
        await SubjectChanged(Actions.Deleted, subject, originPageType, callerConnectionId);

    #endregion

    #region Membership

    private async Task<IEnumerable<Document>> GetDocumentsInSubjectOfMembership(SubjectMembership membership)
    {
        if (membership.Subject != null)
        {
            return await GetDocumentsInSubject(membership.Subject);
        }

        return [];
    }

    private async Task MembershipChanged(
        string action,
        SubjectMembership membership,
        string? originPageType,
        string? callerConnectionId)
    {
        var docIds = (await GetDocumentsInSubjectOfMembership(membership)).Select(e => e.Id).ToArray();

        var targetGroups = NotificationTargets.Membership(membership.Id, membership.SubjectId, membership.UserId, docIds);

        var originGroup = !string.IsNullOrWhiteSpace(originPageType)
            ? HubGroups.Resource(originPageType, membership.Id);
    }

    public async Task MembershipCreated(SubjectMembership membership, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task MembershipUpdated(SubjectMembership membership, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task MembershipDeleted(SubjectMembership membership, string? originPageType = null, string? callerConnectionId = null) { }

    #endregion

    #region Chapter

    public async Task ChapterCreated(Chapter chapter, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task ChapterUpdated(Chapter chapter, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task ChapterDeleted(Chapter chapter, string? originPageType = null, string? callerConnectionId = null) { }

    #endregion

    #region Document

    public async Task DocumentCreated(Document document, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task DocumentUpdated(Document document, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task DocumentDeleted(Document document, string? originPageType = null, string? callerConnectionId = null) { }

    #endregion

    #region Comment

    public async Task CommentCreated(DocumentComment comment, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task CommentUpdated(DocumentComment comment, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task CommentDeleted(DocumentComment comment, string? originPageType = null, string? callerConnectionId = null) { }

    #endregion

    #region ChatSession

    public async Task ChatSessionCreated(ChatSession chatSession, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task ChatSessionUpdated(ChatSession chatSession, string? originPageType = null, string? callerConnectionId = null) { }
    public async Task ChatSessionDeleted(ChatSession chatSession, string? originPageType = null, string? callerConnectionId = null) { }

    #endregion
}

public static class NotificationTargets
{
    public static string[] User(Guid userId, Guid[] docIds) =>
    [
        HubGroups.Resource(PageTypes.Home, userId.ToString()),
        HubGroups.Resource(PageTypes.Profile, userId.ToString()),
        HubGroups.Resource(PageTypes.UserManage),
        HubGroups.Resource(PageTypes.DocumentLibrary),

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentDetails, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentDetails, null)],

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentEdit, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentEdit, null)],
    ];

    public static string[] Membership(Guid membershipId, int subjectId, Guid userId, Guid[] docIds) =>
    [
        HubGroups.Resource(PageTypes.Profile, userId.ToString()),
        HubGroups.Resource(PageTypes.SubjectManage),
        HubGroups.Resource(PageTypes.DocumentLibrary),

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentDetails, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentDetails, null)],

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentEdit, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentEdit, null)],

        HubGroups.Resource(PageTypes.Chat, userId.ToString()),
    ];

    public static string[] Subject(int subjectId, Guid[] userIds, Guid[] docIds) =>
    [
        .. userIds.Length > 0
            ? userIds.Select(id => HubGroups.Resource(PageTypes.Profile, id.ToString()))
            : [HubGroups.Resource(PageTypes.Profile, null)],

        HubGroups.Resource(PageTypes.SubjectManage),
        HubGroups.Resource(PageTypes.DocumentLibrary),

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentDetails, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentDetails, null)],

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentEdit, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentEdit, null)],

        .. userIds.Length > 0
            ? userIds.Select(id => HubGroups.Resource(PageTypes.Chat, id.ToString()))
            : [HubGroups.Resource(PageTypes.Chat, null)],
    ];

    public static string[] Chapter(int chapterId, Guid[] docIds) =>
    [
        HubGroups.Resource(PageTypes.SubjectManage),
        HubGroups.Resource(PageTypes.DocumentLibrary),

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentDetails, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentDetails, null)],

        .. docIds.Length > 0
            ? docIds.Select(id => HubGroups.Resource(PageTypes.DocumentEdit, id.ToString()))
            : [HubGroups.Resource(PageTypes.DocumentEdit, null)],
    ];

    public static string[] Document(Guid docId, int subjectId, Guid uploaderId) =>
    [
        HubGroups.Resource(PageTypes.Profile, uploaderId.ToString()),
        HubGroups.Resource(PageTypes.DocumentLibrary),
        HubGroups.Resource(PageTypes.DocumentDetails, docId.ToString()),
        HubGroups.Resource(PageTypes.DocumentEdit, docId.ToString()),
    ];

    public static string[] Comment(Guid commentId, Guid docId, Guid userId) =>
    [
        HubGroups.Resource(PageTypes.DocumentDetails, docId.ToString()),
    ];

    public static string[] ChatSession(Guid userId) =>
    [
        HubGroups.Resource(PageTypes.Chat, userId.ToString()),
    ];
}
