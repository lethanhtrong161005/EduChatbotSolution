using System.ComponentModel.DataAnnotations.Schema;

namespace Presentation.Models;

public class SubscriptionPlanOptionCardVm
{
    public int Id { get; set; }

    public string OptionName { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    public decimal Price { get; set; } = decimal.Zero;

    public int DurationDays { get; set; }

    public bool IsAvailable { get; set; } = true;
}
