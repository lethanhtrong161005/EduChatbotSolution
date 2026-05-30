using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a payment transaction for a subscription, mapped to the <c>payment_transactions</c> table.
/// </summary>
public class PaymentTransaction : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the <see cref="UserSubscription"/>.</summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>Gets or sets the amount paid.</summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method used (e.g., VNPay, MoMo).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the current payment status.</summary>
    public PaymentStatus PaymentStatus { get; set; }

    /// <summary>Gets or sets the external transaction code from the payment gateway (nullable).</summary>
    public string? TransactionCode { get; set; }

    /// <summary>Gets or sets when the payment was completed (nullable).</summary>
    public DateTime? PaidAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the associated subscription.</summary>
    public virtual UserSubscription Subscription { get; set; } = null!;
}

/// <summary>Defines the possible states of a payment transaction.</summary>
public enum PaymentStatus
{
    /// <summary>Payment has been initiated but not completed.</summary>
    Pending,
    /// <summary>Payment was successfully processed.</summary>
    Paid,
    /// <summary>Payment processing failed.</summary>
    Failed,
    /// <summary>Payment was refunded.</summary>
    Refunded,
}
