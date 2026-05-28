using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public class PaymentTransaction : NaturalEntity
{
    public Guid SubscriptionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;

    [Column(TypeName = "money")]
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
