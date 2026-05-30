using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a subscription plan available to users, mapped to the <c>subscription_plans</c> table.
/// Uses SERIAL (integer) primary key as defined in the database script.
/// </summary>
public class SubscriptionPlan : BaseEntity
{
    /// <summary>Gets or sets the auto-incremented plan identifier.</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Gets or sets the plan name (e.g., Free, Basic, Premium).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the plan description (nullable).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the price in VND.</summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    /// <summary>Gets or sets the maximum number of questions per day.</summary>
    public int DailyQuestionLimit { get; set; }

    /// <summary>Gets or sets whether the plan includes benchmark access.</summary>
    public bool AllowBenchmark { get; set; }

    /// <summary>Gets or sets whether the plan allows using advanced AI models.</summary>
    public bool AllowAdvancedModels { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the subscriptions using this plan.</summary>
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = [];
}
