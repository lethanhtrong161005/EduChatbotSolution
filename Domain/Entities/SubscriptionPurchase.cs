using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class SubscriptionPurchase : NaturalEntity
{
    public int SubscriptionPlanOptionId { get; set; }

    public Guid UserSubscriptionId { get; set; }

    [Column(TypeName = "money")]
    public decimal ChargedAmount { get; set; } = decimal.Zero;

    public DateTime PurchasedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    public virtual SubscriptionPlanOption SubscriptionPlanOption { get; set; } = null!;

    public virtual UserSubscription UserSubscription { get; set; } = null!;

    // ── Convenience ─────────────────────────────────────────
    public SubscriptionPlan SubscriptionPlan => SubscriptionPlanOption.SubscriptionPlan;
    public Guid UserId => UserSubscription.UserId;
}
