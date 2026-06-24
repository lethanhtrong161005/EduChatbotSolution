namespace Presentation.DTOs;

public class CreateChatSessionRequest
{
    public int? SubjectId { get; set; }

    public string MessageContent { get; set; } = string.Empty;
}

public class CreateChatSessionResponse
{
    public Guid SessionId { get; set; }
}
