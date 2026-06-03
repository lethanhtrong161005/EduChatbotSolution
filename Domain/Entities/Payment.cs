using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/// <summary>
/// Represents a payment transaction for a subscription, mapped to the <c>payment_transactions</c> table.
/// </summary>
public class Payment : NaturalEntity
{
    /// <summary>Gets or sets the foreign key to the <see cref="Entities.Order"/>.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the amount paid.</summary>
    [Column(TypeName = "money")]
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method used (e.g., VNPay, MoMo).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets or sets the current payment status.</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>Gets or sets the internal transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the external transaction code.</summary>
    public string? ExternalTransactionCode { get; set; }

    /// <summary>Gets or sets when the payment was completed (nullable).</summary>
    public DateTime? PaidAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the associated order.</summary>
    public virtual Order Order { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending,
    Fulfilled,
    Failed,
    Cancelled,
    Refunded,
}
