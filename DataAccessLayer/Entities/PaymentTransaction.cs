namespace DataAccessLayer.Entities;

public class PaymentTransaction : UuidEntity
{
    public Guid SubscriptionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public DateTime PaidAt { get; set; }

    public virtual UserSubscription Subscription { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending,
    Fulfilled,
    Failed,
}
