namespace Presentation.RealtimeNotif;

public static class HubGroups
{
    public const string DocumentLibrary = "doc-lib";

    public static string DocumentDetails(Guid id) => $"doc:{id}";
}
