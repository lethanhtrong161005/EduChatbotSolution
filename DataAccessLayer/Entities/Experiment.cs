namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a RAG evaluation experiment, mapped to the <c>experiments</c> table.
/// </summary>
public class Experiment : NaturalEntity
{
    /// <summary>Gets or sets the experiment name.</summary>
    public string ExperimentName { get; set; } = string.Empty;

    /// <summary>Gets or sets the text embedding model used (e.g., text-embedding-ada-002).</summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>Gets or sets the chunking strategy used (e.g., fixed-size, semantic).</summary>
    public string ChunkStrategy { get; set; } = string.Empty;

    /// <summary>Gets or sets the retrieval method used (e.g., cosine similarity).</summary>
    public string RetrievalMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the LLM used for generation (e.g., gpt-4o).</summary>
    public string LlmModel { get; set; } = string.Empty;

    /// <summary>Gets or sets the average RAGAS score achieved (nullable).</summary>
    public double? AverageRagasScore { get; set; }

    /// <summary>Gets or sets additional notes about this experiment (nullable).</summary>
    public string? Notes { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the individual question responses for this experiment.</summary>
    public virtual ICollection<TestResponse> TestResponses { get; set; } = [];
}
