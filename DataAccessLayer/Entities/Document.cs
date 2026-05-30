namespace DataAccessLayer.Entities;

/// <summary>
/// Represents an uploaded document, mapped to the <c>documents</c> table.
/// </summary>
public class Document : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the parent <see cref="Subject"/>.</summary>
    public int SubjectId { get; set; }

    /// <summary>Gets or sets the optional foreign key to a <see cref="Chapter"/>.</summary>
    public int? ChapterId { get; set; }

    /// <summary>Gets or sets the foreign key to the <see cref="ApplicationUser"/> who uploaded.</summary>
    public Guid UploadedBy { get; set; }

    /// <summary>Gets or sets the stored file name (server-side).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the original file name as provided by the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the file MIME type or extension (e.g., pdf).</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long? FileSize { get; set; }

    /// <summary>Gets or sets the server path to the stored file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the document has been indexed for vector search.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Gets or sets when the document was uploaded.</summary>
    public DateTime UploadedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the subject this document belongs to.</summary>
    public virtual Subject Subject { get; set; } = null!;

    /// <summary>Gets or sets the chapter this document belongs to (optional).</summary>
    public virtual Chapter? Chapter { get; set; }

    /// <summary>Gets or sets the user who uploaded this document.</summary>
    public virtual ApplicationUser Uploader { get; set; } = null!;

    /// <summary>Gets or sets the chunks generated from this document.</summary>
    public virtual ICollection<Chunk> Chunks { get; set; } = [];
}
