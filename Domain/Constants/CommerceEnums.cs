namespace Domain.Constants;

public enum OrderType
{
    New,
    Upgrade,
    Downgrade,
    Renewal,
}

public enum PaymentMethod
{
    BankTransfer,
    Visa_Mastercard,
    VnPay,
    MoMo,
    ZaloPay,
}
