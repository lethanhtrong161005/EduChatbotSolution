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
    PendingPayment,
    Upcoming,
    Active,
    Superseded,
    Expired,
    Cancelled,
    Promo,
}

public enum OrderType
{
    New,
    Upgrade,
    Downgrade,
    Renewal,
}

public enum OrderStatus
{
    PendingPayment,
    Completed,
    Failed,
    Cancelled,
    //Refunded,
}

public enum PaymentMethod
{
    BankTransfer,
    Visa_Mastercard,
    VnPay,
    MoMo,
    ZaloPay,
}

public enum PaymentStatus
{
    Pending,
    Fulfilled,
    Failed,
    Cancelled,
    //Refunded,
}

public enum SenderRole
{
    User,
    Assistant,
    System,
}
