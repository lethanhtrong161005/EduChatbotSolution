using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Order : NaturalEntity
{
    public Guid SubscriptionId { get; set; }

    [Column(TypeName = "money")]
    public decimal ChargedAmount { get; set; }

    public OrderStatus Status { get; set; }

    // ── Navigation ──────────────────────────────────────────
    public virtual Subscription Subscription { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = [];

    // ── Convenience ─────────────────────────────────────────
    public Guid UserId => Subscription.UserId;
    public PlanOption PlanOption => Subscription.PlanOption;
    public Plan Plan => PlanOption.Plan;
}
