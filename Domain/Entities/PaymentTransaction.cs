using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Represents a payment transaction for a subscription, mapped to the <c>payment_transactions</c> table.
/// </summary>
public class PaymentTransaction : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the <see cref="SubscriptionPurchase"/>.</summary>
    public Guid SubscriptionPurchaseId { get; set; }

    /// <summary>Gets or sets the amount paid.</summary>
    [Column(TypeName = "money")]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method used (e.g., VNPay, MoMo).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the current payment status.</summary>
    public PaymentStatus PaymentStatus { get; set; }

    /// <summary>Gets or sets the internal transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>Gets or sets when the payment was completed (nullable).</summary>
    public DateTime? PaidAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the associated purchase.</summary>
    public virtual SubscriptionPurchase SubscriptionPurchase { get; set; } = null!;
}
