namespace Domain.Entities;

/// <summary>
/// Represents a subject (course), mapped to the <c>subjects</c> table.
/// Uses SERIAL (integer) primary key as defined in the database script.
/// </summary>
public class Subject : NaturalEntity
{
    /// <summary>Gets or sets the unique subject code (e.g., SE101).</summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the full subject name.</summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the subject.</summary>
    public string? Description { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the chapters belonging to this subject.</summary>
    public virtual ICollection<Chapter> Chapters { get; set; } = [];
}
