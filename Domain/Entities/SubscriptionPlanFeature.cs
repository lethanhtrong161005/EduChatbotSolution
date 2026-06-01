namespace Domain.Entities;

public class SubscriptionPlanFeature : CategoryLikeEntity
{
    public int SubscriptionPlanId { get; set; }

    public int FeatureKey { get; set; }

    public string FeatureName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string FeatureValue { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
