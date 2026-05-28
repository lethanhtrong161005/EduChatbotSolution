using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public class SubscriptionPlan : CategoryLikeEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    public decimal Price { get; set; } = decimal.Zero;

    public int DailyChatLimit { get; set; }
    public bool AllowFileUpload { get; set; }
    public bool AllowAdvancedModels { get; set; }
}
