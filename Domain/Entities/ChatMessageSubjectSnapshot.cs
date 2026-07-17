namespace Domain.Entities;

public class ChatMessageSubjectSnapshot : NaturalEntity
{
    public Guid ChatMessageId { get; set; }
    public int SubjectOrder { get; set; }
    public int? SubjectId { get; set; }
    public int SourceSubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;

    public virtual ChatMessage ChatMessage { get; set; } = null!;
    public virtual Subject? Subject { get; set; }
}
