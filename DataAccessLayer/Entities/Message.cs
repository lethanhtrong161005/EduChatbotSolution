namespace DataAccessLayer.Entities;

public class Message : NaturalEntity
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public SenderRole SenderRole { get; set; }
    public int PromptTokenCount { get; set; }
    public int CompletionTokenCount { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
}

public enum SenderRole
{
    User,
    Assistant,
    System,
}
