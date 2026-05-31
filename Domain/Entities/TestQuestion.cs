namespace Domain.Entities;

/// <summary>
/// Represents a question used in evaluation tests, mapped to the <c>test_questions</c> table.
/// Uses UUID primary key as defined in the database script.
/// </summary>
public class TestQuestion : CategoryLikeEntity
{
    /// <summary>Gets or sets the question text.</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected ground truth answer.</summary>
    public string GroundTruth { get; set; } = string.Empty;

    /// <summary>Gets or sets the difficulty level (nullable).</summary>
    public string? Difficulty { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the experiment responses for this question.</summary>
    public virtual ICollection<TestResponse> TestResponses { get; set; } = [];
}
