using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a chapter within a subject, mapped to the <c>chapters</c> table.
/// Uses SERIAL (integer) primary key as defined in the database script.
/// </summary>
public class Chapter : BaseEntity
{
    /// <summary>Gets or sets the auto-incremented chapter identifier.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Gets or sets the foreign key to the parent <see cref="Subject"/>.</summary>
    public int SubjectId { get; set; }

    /// <summary>Gets or sets the chapter name.</summary>
    public string ChapterName { get; set; } = string.Empty;

    /// <summary>Gets or sets optional chapter number for ordering.</summary>
    public int? ChapterNumber { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent subject.</summary>
    public virtual Subject Subject { get; set; } = null!;

    /// <summary>Gets or sets the documents uploaded under this chapter.</summary>
    public virtual ICollection<Document> Documents { get; set; } = [];
}
