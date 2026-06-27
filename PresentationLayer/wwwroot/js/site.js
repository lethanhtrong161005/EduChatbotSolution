const HubMethod = Object.freeze({
    JoinResourceType: "SubscribeToResourceType",
    JoinResource: "SubscribeToResource",
    JoinResourceCollection: "SubscribeToResourceCollection",

    LeaveResourceType: "UnsubscribeFromResourceType",
    LeaveResource: "UnsubscribeFromResource",
    LeaveResourceCollection: "UnsubscribeFromResourceCollection",
});

const ResourceType = Object.freeze({
    User: "user",
    Membership: "membership",
    Subject: "subject",
    Chapter: "chapter",
    Document: "document",
    Comment: "comment",
    ChatSession: "chat-session",
});

const ResourceAction = Object.freeze({
    Created: "created",
    Updated: "updated",
    Deleted: "deleted",
    Disabled: "disabled",
    Enabled: "enabled",
});
