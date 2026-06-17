namespace Domain.Entities;

/// <summary>
/// Represents a conversation session, mapped to the <c>conversations</c> table.
/// </summary>
public class ChatSession : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the owning <see cref="ApplicationUser"/>.</summary>
    public Guid UserId { get; set; }
    public int SubjectId { get; set; }

    /// <summary>Gets or sets the optional conversation title.</summary>
    public string? Title { get; set; }

    // UpdatedAt is inherited from BaseEntity

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the user who owns this conversation.</summary>
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;

    /// <summary>Gets or sets the messages in this conversation.</summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = [];
}
