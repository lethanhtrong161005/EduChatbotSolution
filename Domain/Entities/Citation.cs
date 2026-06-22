namespace Domain.Entities;

/// <summary>
/// Represents a citation linking a message to a source document chunk,
/// mapped to the <c>citations</c> table.
/// </summary>
public class Citation : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the <see cref="Entities.ChatMessage"/> that contains this citation.</summary>
    public Guid ChatMessageId { get; set; }

    /// <summary>Gets or sets the foreign key to the specific <see cref="Entities.Chunk"/>.</summary>
    public Guid ChunkId { get; set; }

    public int CitationIndex { get; set; }

    /// <summary>Gets or sets the quoted text from the chunk (nullable).</summary>
    public string QuotedText { get; set; } = string.Empty;

    public double SimilarityScore { get; set; }

    public string? LocationInDocument { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent message.</summary>
    public virtual ChatMessage ChatMessage { get; set; } = null!;

    /// <summary>Gets or sets the source chunk.</summary>
    public virtual Chunk Chunk { get; set; } = null!;
}
