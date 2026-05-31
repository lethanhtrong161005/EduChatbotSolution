using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class SubscriptionPlanOption : CategoryLikeEntity
{
    public int SubscriptionPlanId { get; set; }

    public string OptionName { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    public decimal Price { get; set; } = decimal.Zero;

    public int DurationDays { get; set; }

    public bool IsAvailable { get; set; } = true;

    // ── Navigation ──────────────────────────────────────────
    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
