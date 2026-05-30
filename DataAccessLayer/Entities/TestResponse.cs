namespace DataAccessLayer.Entities;

/// <summary>
/// Represents an individual RAG evaluation result, mapped to the <c>experiment_results</c> table.
/// </summary>
public class TestResponse : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the <see cref="Experiment"/>.</summary>
    public Guid ExperimentId { get; set; }

    /// <summary>Gets or sets the foreign key to the <see cref="TestQuestion"/>.</summary>
    public Guid TestQuestionId { get; set; }

    /// <summary>Gets or sets the generated answer from the RAG pipeline (nullable).</summary>
    public string? GeneratedAnswer { get; set; }

    /// <summary>Gets or sets the RAGAS faithfulness score (nullable).</summary>
    public double? Faithfulness { get; set; }

    /// <summary>Gets or sets the RAGAS answer relevancy score (nullable).</summary>
    public double? AnswerRelevancy { get; set; }

    /// <summary>Gets or sets the RAGAS context precision score (nullable).</summary>
    public double? ContextPrecision { get; set; }

    /// <summary>Gets or sets the RAGAS context recall score (nullable).</summary>
    public double? ContextRecall { get; set; }

    /// <summary>Gets or sets the total generation latency in milliseconds (nullable).</summary>
    public double? LatencyMs { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the parent experiment.</summary>
    public virtual Experiment Experiment { get; set; } = null!;

    /// <summary>Gets or sets the test question this response answers.</summary>
    public virtual TestQuestion TestQuestion { get; set; } = null!;
}
