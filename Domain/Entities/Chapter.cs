namespace Domain.Entities;

/// <summary>
/// Represents a chapter within a subject, mapped to the <c>chapters</c> table.
/// Uses SERIAL (integer) primary key as defined in the database script.
/// </summary>
public class Chapter : CategoryLikeEntity
{
    /// <summary>Gets or sets the foreign key to the parent <see cref="Entities.Subject"/>.</summary>
    public int SubjectId { get; set; }

    /// <summary>Gets or sets chapter number for ordering.</summary>
    public int ChapterNumber { get; set; }

    /// <summary>Gets or sets the chapter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the chapter description (optional).</summary>
    public string? Description { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent subject.</summary>
    public virtual Subject Subject { get; set; } = null!;

    /// <summary>Gets or sets the document associations for this chapter.</summary>
    public virtual ICollection<DocumentChapter> DocumentChapters { get; } = [];

    /// <summary>Skip navigation for document associations.</summary>
    public virtual ICollection<Document> Documents { get; } = [];
}
