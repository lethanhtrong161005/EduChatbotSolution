namespace Presentation.RealtimeWeb;

public static class HubGroups
{
    public const string DocumentLibrary = "doc-lib";

    public static string DocumentDetails(Guid docId) => $"doc:{docId}";

    public static string Chat(Guid sessionId) => $"chat:{sessionId}";

    public static string Resource(string pageType, string? resourceId = null) => $"res:{pageType}:{resourceId}";
}
