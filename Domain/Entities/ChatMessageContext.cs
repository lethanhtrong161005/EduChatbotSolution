namespace Domain.Entities;

public class ChatMessageContext : NaturalEntity
{
    public Guid ChatMessageId { get; set; }
    public int ContextIndex { get; set; }
    public string ContextText { get; set; } = string.Empty;

    public virtual ChatMessage ChatMessage { get; set; } = null!;
}
