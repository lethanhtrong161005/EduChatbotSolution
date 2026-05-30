namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a citation linking a message to a source document chunk,
/// mapped to the <c>citations</c> table.
/// </summary>
public class Citation : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the <see cref="Message"/> that contains this citation.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the foreign key to the source <see cref="Document"/>.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Gets or sets the foreign key to the specific <see cref="Chunk"/>.</summary>
    public Guid ChunkId { get; set; }

    /// <summary>Gets or sets the quoted text from the chunk (nullable).</summary>
    public string? QuotedText { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent message.</summary>
    public virtual Message Message { get; set; } = null!;

    /// <summary>Gets or sets the source document.</summary>
    public virtual Document Document { get; set; } = null!;

    /// <summary>Gets or sets the source chunk.</summary>
    public virtual Chunk Chunk { get; set; } = null!;
}
