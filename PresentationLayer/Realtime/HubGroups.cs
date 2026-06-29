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

    public static string ResourceCollection(string principalType, string? principleId, string dependentType) => $"resource:{principalType}:{principleId ?? "*"}:{dependentType}-collection";

    public static string ResourceCollection(string principalType, string dependentType) => ResourceCollection(principalType, null, dependentType);

    #endregion

    #region CommentHub

    public static string CommentSection(string documentId) => $"document:{documentId}:comments";

    #endregion
}
