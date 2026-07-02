using System.Text.Json.Serialization;

namespace Domain.Contracts.DTOs;

public record ResourceUpdate
{
    [JsonConverter(typeof(JsonStringEnumConverter<ResourceType>))]
    public required ResourceType ResourceType { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<ResourceAction>))]
    public required ResourceAction Action { get; init; }

    public required string ResourceId { get; init; }

    public string? ResourceName { get; init; }

    public Dictionary<string, object?> Properties { get; init; } = [];
}

public enum ResourceType
{
    [JsonStringEnumMemberName("user")]
    User,

    [JsonStringEnumMemberName("membership")]
    Membership,

    [JsonStringEnumMemberName("subject")]
    Subject,

    [JsonStringEnumMemberName("chapter")]
    Chapter,

    [JsonStringEnumMemberName("document")]
    Document,

    [JsonStringEnumMemberName("document-chapter")]
    DocumentChapter,

    // Comments are manage by CommentHub

    [JsonStringEnumMemberName("chat-session")]
    ChatSession,

    // ChatMessages are managed by AiChatHub
}

public enum ResourceAction
{
    [JsonStringEnumMemberName("created")]
    Created,

    [JsonStringEnumMemberName("updated")]
    Updated,

    [JsonStringEnumMemberName("deleted")]
    Deleted,

    [JsonStringEnumMemberName("disabled")]
    Disabled,

    [JsonStringEnumMemberName("enabled")]
    Enabled,
}
