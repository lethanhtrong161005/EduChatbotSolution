namespace Presentation.SignalR;

public static class HubGroups
{
    public const string DocumentLibrary = "doc-lib";

    public static string DocumentDetails(Guid docId) => $"doc:{docId}";

    public static string Chat(Guid userId) => $"chat:{userId}";
}
