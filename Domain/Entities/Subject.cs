namespace Domain.Entities;

/// <summary>
/// Represents a subject (course), mapped to the <c>subjects</c> table.
/// Uses SERIAL (integer) primary key as defined in the database script.
/// </summary>
public class Subject : CategoryLikeEntity
{
    /// <summary>Gets or sets the unique subject code (e.g., SE101).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the full subject name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the subject.</summary>
    public string? Description { get; set; }

    // ── Navigation ──────────────────────────────────────────
    public virtual SubjectAiConfiguration? AiConfiguration { get; set; }

    public virtual SubjectStorageConfiguration? StorageConfiguration { get; set; }

    /// <summary>Gets or sets the chapters belonging to this subject.</summary>
    public virtual ICollection<Chapter> Chapters { get; } = [];

    public virtual ICollection<Membership> Memberships { get; } = [];

    public virtual ICollection<ApplicationUser> Members { get; } = [];
}
