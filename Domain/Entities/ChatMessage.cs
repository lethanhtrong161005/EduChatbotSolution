namespace Domain.Entities;

/// <summary>
/// Represents a chat message within a conversation, mapped to the <c>messages</c> table.
/// </summary>
public class ChatMessage : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the parent <see cref="Conversation"/>.</summary>
    public Guid ChatSessionId { get; set; }

    /// <summary>Gets or sets the role of the sender (User, Assistant, or System).</summary>
    public ChatRole ChatRole { get; set; }

    /// <summary>Gets or sets the message text content.</summary>
    public string Content { get; set; } = string.Empty;

    public string? LlmModel { get; set; }

    public double? GenerationTemperature { get; set; }

    /// <summary>Gets or sets the number of prompt tokens consumed (nullable).</summary>
    public int? PromptTokens { get; set; }

    /// <summary>Gets or sets the number of completion tokens generated (nullable).</summary>
    public int? CompletionTokens { get; set; }

    public long? ResponseTimeMs { get; set; }

    public int? RetrievedChunkCount { get; set; }

    /// <summary>Gets or sets when the message was sent.</summary>
    public DateTime SentAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent conversation.</summary>
    public virtual ChatSession ChatSession { get; set; } = null!;

    /// <summary>Gets or sets citations associated with this message.</summary>
    public virtual ICollection<Citation> Citations { get; set; } = [];
}

public enum ChatRole
{
    User,
    Assistant,
    System,
}
