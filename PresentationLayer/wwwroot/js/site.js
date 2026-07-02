const HubMethod = Object.freeze({
    JoinResourceType: "SubscribeToResourceType",
    JoinResource: "SubscribeToResource",
    JoinResourceTypeCollection: "SubscribeToResourceTypeCollection",
    JoinResourceCollection: "SubscribeToResourceCollection",

    LeaveResourceType: "UnsubscribeFromResourceType",
    LeaveResource: "UnsubscribeFromResource",
    LeaveResourceTypeCollection: "UnsubscribeFromResourceTypeCollection",
    LeaveResourceCollection: "UnsubscribeFromResourceCollection",
});

const ResourceType = Object.freeze({
    User: "user",
    Membership: "membership",
    Subject: "subject",
    Chapter: "chapter",
    Document: "document",
    DocumentChapter: "document-chapter",
    ChatSession: "chat-session",
});

const ResourceAction = Object.freeze({
    Created: "created",
    Updated: "updated",
    Deleted: "deleted",
    Disabled: "disabled",
    Enabled: "enabled",
});
