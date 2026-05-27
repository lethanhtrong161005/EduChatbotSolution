namespace DataAccessLayer.Entities;

public class SubscriptionPlan : CategoryEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; } = decimal.Zero;
    public int DailyChatLimit { get; set; }
    public bool AllowFileUpload { get; set; }
    public bool AllowAdvancedModels { get; set; }
}
