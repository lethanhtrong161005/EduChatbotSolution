namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a user's subscription to a plan, mapped to the <c>user_subscriptions</c> table.
/// </summary>
public class UserSubscription : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the subscribed <see cref="ApplicationUser"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the foreign key to the <see cref="SubscriptionPlan"/>.</summary>
    public int PlanId { get; set; }

    /// <summary>Gets or sets the subscription start date.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Gets or sets the subscription end date.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Gets or sets the current subscription status.</summary>
    public SubscriptionStatus Status { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the subscribed user.</summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>Gets or sets the subscription plan.</summary>
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    /// <summary>Gets or sets the payment transactions for this subscription.</summary>
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}

/// <summary>Defines the lifecycle states of a user subscription.</summary>
public enum SubscriptionStatus
{
    /// <summary>Awaiting payment or activation.</summary>
    Pending,
    /// <summary>Currently active.</summary>
    Active,
    /// <summary>The subscription period has ended.</summary>
    Expired,
    /// <summary>Cancelled by the user or system.</summary>
    Cancelled,
}
