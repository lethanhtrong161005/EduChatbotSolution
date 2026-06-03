namespace Domain.Entities;

/// <summary>
/// Represents a user's subscription to a plan option, mapped to the <c>user_subscriptions</c> table.
/// </summary>
public class Subscription : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the subscribed <see cref="ApplicationUser"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the foreign key to the associated <see cref="PlanOption"/>.</summary>
    public int PlanOptionId { get; set; }

    /// <summary>Gets or sets the subscription start date.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Gets or sets the subscription end date.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Gets or sets the current subscription status.</summary>
    public SubscriptionStatus Status { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the subscribed user.</summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>Gets or sets the associated plan option.</summary>
    public virtual PlanOption PlanOption { get; set; } = null!;

    // Currently, we have a 0..1:1 relationship between Order and Subscription.
    // Conceptually, it should be 1:1, but EF core does not support deferrability or circular ref (as of 2026-05).
    // cf. https://github.com/dotnet/efcore/issues/11903
    /// <summary>Gets or sets the associated order (nullable).</summary>
    public virtual Order? Order { get; set; }

    // ── Convenience ─────────────────────────────────────────
    public Plan Plan => PlanOption.Plan;
}

public enum SubscriptionStatus
{
    PendingPayment,
    Upcoming,
    Active,
    Superseded,
    Expired,
    Cancelled,
    Promo,
}
