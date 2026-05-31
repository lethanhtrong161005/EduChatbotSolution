using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a chat message within a conversation, mapped to the <c>messages</c> table.
/// </summary>
public class Message : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the parent <see cref="Conversation"/>.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>Gets or sets the role of the sender (User, Assistant, or System).</summary>
    public SenderRole SenderRole { get; set; }

    /// <summary>Gets or sets the message text content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of prompt tokens consumed (nullable).</summary>
    public int? PromptTokens { get; set; }

    /// <summary>Gets or sets the number of completion tokens generated (nullable).</summary>
    public int? CompletionTokens { get; set; }

    /// <summary>Gets or sets when the message was sent.</summary>
    public DateTime SentAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent conversation.</summary>
    public virtual Conversation Conversation { get; set; } = null!;

    /// <summary>Gets or sets citations associated with this message.</summary>
    public virtual ICollection<Citation> Citations { get; set; } = [];
}
