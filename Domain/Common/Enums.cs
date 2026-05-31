namespace Domain.Common;

public enum UserRole
{
    Student,
    Lecturer,
    Admin,
}

public enum PaymentMethod
{
    BankTransfer,
    PayPal,
    ZaloPay,
}

public enum PaymentStatus
{
    Pending,
    Fulfilled,
    Failed,
}

public enum SubscriptionStatus
{
    Cancelled = -1,
    PendingPayment,
    Upcoming,
    Active,
    Superceded,
    Expired,
}

public enum PurchaseType
{
    New,
    Upgrade,
    Downgrade,
    Renewal,
}

public enum SenderRole
{
    User,
    Assistant,
    System,
}
