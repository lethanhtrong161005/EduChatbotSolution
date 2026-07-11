namespace Domain.Entities;

/// <summary>
/// Represents an uploaded document, mapped to the <c>documents</c> table.
/// </summary>
public class Document : NaturalEntity
{
    /// <summary>Gets or sets the optional foreign key to a <see cref="Subject"/>.</summary>
    public int SubjectId { get; set; }

    /// <summary>Gets or sets the foreign key to the <see cref="ApplicationUser"/> who uploaded.</summary>
    public Guid UploaderId { get; set; }

    /// <summary>Gets or sets the document title.</summary>
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the original file name as provided by the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the file MIME type or extension (e.g., pdf).</summary>
    public DocumentType FileType { get; set; }

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long? FileSize { get; set; }

    /// <summary>Gets or sets the opaque locator in durable storage.</summary>
    public string? StorageLocator { get; set; }

    /// <summary>Gets or sets the opaque locator in staging storage.</summary>
    public string? StagingLocator { get; set; }

    /// <summary>Gets or sets the backend used to store the document file.</summary>
    public DocumentStorageMethod StorageMethod { get; set; } = DocumentStorageMethod.Unspecified;

    /// <summary>Gets or sets whether the document has been indexed for vector search.</summary>
    public DocumentStatus Status { get; set; }

    public string? ParserUsed { get; set; }

    public string? IndexingErrors { get; set; }

    /// <summary>Gets or sets when the document was uploaded.</summary>
    public DateTime UploadedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the subject this document belongs to.</summary>
    public virtual Subject Subject { get; set; } = null!;

    /// <summary>Gets or sets the user who uploaded this document.</summary>
    public virtual ApplicationUser Uploader { get; set; } = null!;

    /// <summary>Gets or sets the chapter associations for this document.</summary>
    public virtual ICollection<DocumentChapter> DocumentChapters { get; } = [];

    /// <summary>Skip navigation for chapter associations.</summary>
    public virtual ICollection<Chapter> Chapters { get; } = [];

    public virtual ICollection<ParsedSection> ParsedSections { get; } = [];

    /// <summary>Gets or sets the chunks generated from this document.</summary>
    public virtual ICollection<Chunk> Chunks { get; } = [];

    public virtual ICollection<DocumentComment> Comments { get; } = [];

    public string ContentType => FileType switch
    {
        DocumentType.TXT => "text/plain",
        DocumentType.DOCX => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        DocumentType.PDF => "application/pdf",
        DocumentType.HTML => "text/html",
        DocumentType.PPTX => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        _ => "application/octet-stream",
    };
}

public enum DocumentType
{
    TXT,
    DOCX,
    PDF,
    HTML,
    PPTX,
    Other,
}

public enum DocumentStorageMethod
{
    Unspecified = 0,
    LocalHardDrive = 1,
    Supabase = 2,
}

public enum DocumentStatus
{
    Failed = -1,
    Received = 0,
    Parsing,
    Parsed,
    Chunking,
    Chunked,
    Embedding,
    Indexed,
}
