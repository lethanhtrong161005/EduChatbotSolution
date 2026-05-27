namespace DataAccessLayer.Entities;

public class UserSubscription : UuidEntity
{
    public Guid UserId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }

    public virtual ICollection<User> Users { get; set; } = [];
    public virtual ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = [];
}

public enum SubscriptionStatus
{
    Pending,
    Active,
    Expired,
    Cancelled,
}
