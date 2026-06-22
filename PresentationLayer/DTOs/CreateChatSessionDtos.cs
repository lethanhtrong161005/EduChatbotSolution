namespace Presentation.DTOs;

public class CreateChatSessionRequest
{
    public int? SubjectId { get; set; }
}

public class CreateChatSessionResponse
{
    public Guid SessionId { get; set; }
}
