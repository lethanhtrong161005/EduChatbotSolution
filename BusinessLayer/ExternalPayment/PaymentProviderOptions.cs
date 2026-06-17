namespace Business.ExternalPayment;

public class PaymentProviderOptions
{
    public ZaloPaySettings ZaloPay { get; set; } = new();
}

public class ZaloPaySettings
{
    public int AppId { get; set; }
    public string Key1 { get; set; } = string.Empty;
    public string Key2 { get; set; } = string.Empty;
    public string CreateTransactionEndpoint { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string RedirectUrlBase { get; set; } = string.Empty;
    public string QueryTransactionEndpoint { get; set; } = string.Empty;
    public string BankListEndpoint { get; set; } = string.Empty;
}
