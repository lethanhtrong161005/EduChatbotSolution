namespace Domain.Entities;

public class ChatMessageRequestMessage : NaturalEntity
{
    public Guid ChatMessageId { get; set; }
    public int MessageOrder { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public virtual ChatMessage ChatMessage { get; set; } = null!;
}
