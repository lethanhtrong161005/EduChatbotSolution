namespace Domain.Entities;

/// <summary>
/// Represents a chat message within a conversation, mapped to the <c>chat_messages</c> table.
/// </summary>
public class ChatMessage : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the parent <see cref="Entities.ChatSession"/>.</summary>
    public Guid ChatSessionId { get; set; }

    /// <summary>Gets or sets the role of the sender (User, Assistant, or System).</summary>
    public ChatRole ChatRole { get; set; }

    /// <summary>Gets or sets the message text content.</summary>
    public string Content { get; set; } = string.Empty;

    public string RawContent { get; set; } = string.Empty;

    /// <summary>Gets or sets when the message was sent.</summary>
    public DateTime SentAt { get; set; }

    public MessageStatus Status { get; set; }

    public string? GenerationErrors { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent conversation.</summary>
    public virtual ChatSession ChatSession { get; set; } = null!;

    public virtual ChatMessageGenerationSettings? GenerationSettings { get; set; }

    public virtual ChatMessageGenerationMetrics? GenerationMetrics { get; set; }

    /// <summary>Gets or sets citations associated with this message.</summary>
    public virtual ICollection<Citation> Citations { get; } = [];
}

public enum ChatRole
{
    System,
    User,
    Assistant,
}

public enum MessageStatus
{
    Pending,
    Generating,
    Completed,
    Failed,
}
