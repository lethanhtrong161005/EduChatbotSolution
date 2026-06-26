namespace Presentation.RealtimeWeb;

public static class HubGroups
{
    public static string DocumentStatusLibrary() => "doc-status:library";

    public static string DocumentStatusDetails(Guid docId) => $"doc-status:{docId}";

    public static string Chat(Guid sessionId) => $"chat:{sessionId}";

    public static string Resource(string pageType, string? resourceId = null) => $"realtime-resource:{pageType}:{resourceId}";
}
