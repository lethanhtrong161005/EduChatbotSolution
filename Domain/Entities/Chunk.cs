namespace Domain.Entities;

/// <summary>
/// Represents a text chunk from a document, mapped to the <c>document_chunks</c> table.
/// </summary>
public class Chunk : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the parent <see cref="Document"/>.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Gets or sets the sequential index of this chunk within its document.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>Gets or sets the raw text content of this chunk.</summary>
    public string ChunkText { get; set; } = string.Empty;

    /// <summary>Gets or sets the embedding model used to vectorize this chunk.</summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>Gets or sets the chunking strategy used (e.g., fixed-size, semantic).</summary>
    public string ChunkStrategy { get; set; } = string.Empty;

    /// <summary>Gets or sets the vector store ID for retrieval (nullable).</summary>
    public string? VectorId { get; set; }

    /// <summary>Gets or sets the number of tokens in this chunk (nullable).</summary>
    public int? TokenCount { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent document.</summary>
    public virtual Document Document { get; set; } = null!;

    /// <summary>Gets or sets the citations that reference this chunk.</summary>
    public virtual ICollection<Citation> Citations { get; set; } = [];
}
