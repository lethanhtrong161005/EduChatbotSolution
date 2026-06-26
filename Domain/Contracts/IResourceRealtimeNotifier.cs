using Domain.Entities;

namespace Domain.Contracts;

public interface IResourceRealtimeNotifier
{
    Task UserCreated(ApplicationUser user, string? originPageType, string? callerConnectionId = null);
    Task UserUpdated(ApplicationUser user, string? originPageType, string? callerConnectionId = null);
    Task UserDeleted(ApplicationUser user, string? originPageType, string? callerConnectionId = null);
    Task UserDisabled(ApplicationUser user, string? originPageType, string? callerConnectionId = null);
    Task UserReactiviated(ApplicationUser user, string? originPageType, string? callerConnectionId = null);

    Task SubjectCreated(Subject subject, string? originPageType, string? callerConnectionId = null);
    Task SubjectUpdated(Subject subject, string? originPageType, string? callerConnectionId = null);
    Task SubjectDeleted(Subject subject, string? originPageType, string? callerConnectionId = null);

    Task MembershipCreated(SubjectMembership membership, string? originPageType, string? callerConnectionId = null);
    Task MembershipUpdated(SubjectMembership membership, string? originPageType, string? callerConnectionId = null);
    Task MembershipDeleted(SubjectMembership membership, string? originPageType, string? callerConnectionId = null);

    Task ChapterCreated(Chapter chapter, string? originPageType, string? callerConnectionId = null);
    Task ChapterUpdated(Chapter chapter, string? originPageType, string? callerConnectionId = null);
    Task ChapterDeleted(Chapter chapter, string? originPageType, string? callerConnectionId = null);

    Task DocumentCreated(Document document, string? originPageType, string? callerConnectionId = null);
    Task DocumentUpdated(Document document, string? originPageType, string? callerConnectionId = null);
    Task DocumentDeleted(Document document, string? originPageType, string? callerConnectionId = null);

    Task CommentCreated(DocumentComment comment, string? originPageType, string? callerConnectionId = null);
    Task CommentUpdated(DocumentComment comment, string? originPageType, string? callerConnectionId = null);
    Task CommentDeleted(DocumentComment comment, string? originPageType, string? callerConnectionId = null);

    Task ChatSessionCreated(ChatSession chatSession, string? originPageType, string? callerConnectionId = null);
    Task ChatSessionUpdated(ChatSession chatSession, string? originPageType, string? callerConnectionId = null);
    Task ChatSessionDeleted(ChatSession chatSession, string? originPageType, string? callerConnectionId = null);
}
