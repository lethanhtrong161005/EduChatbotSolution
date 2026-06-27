namespace Presentation.Realtime;

public static class HubGroups
{
    #region DocumentStatusHub

    public static string DocumentStatus(Guid? documentId = null) => $"{(documentId == null ? "documents" : $"document:{documentId}")}:status";

    #endregion

    #region AiChatHub

    public static string Chat(Guid sessionId) => $"chat:{sessionId}";

    #endregion

    #region ResourceHub

    public static string Resource(string resourceType, string? resourceId = null) => $"resource:{resourceType}:{resourceId ?? "*"}";

    public static string ResourceCollections(string principalType, string principalId, string dependentType) => $"resource:{principalType}:{principalId}:{dependentType}-collection";

    #endregion

    #region CommentHub

    public static string CommentSection(string documentId) => $"document:{documentId}:comments";

    #endregion
}
