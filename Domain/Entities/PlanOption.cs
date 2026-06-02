using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class PlanOption : CategoryLikeEntity
{
    public int PlanId { get; set; }

    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public bool IsAvailable { get; set; } = true;

    // ── Navigation ──────────────────────────────────────────
    public virtual Plan Plan { get; set; } = null!;
}
