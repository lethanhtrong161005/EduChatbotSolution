namespace Domain.Common;

public enum UserRole
{
    Student,
    Lecturer,
    Admin,
}

public enum PlanFeature
{
    ChatLimit,
    ChatSession,
    FileUpload,
    FileLibrary,
    AdvancedModel,
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

public enum SenderRole
{
    User,
    Assistant,
    System,
}
