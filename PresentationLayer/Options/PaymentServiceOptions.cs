namespace Presentation.Options;

public class PaymentServiceOptions
{
    public ZaloPaySettings ZaloPay { get; set; } = new();
}

public class ZaloPaySettings
{
    public int AppId { get; set; }
    public string Key1 { get; set; } = string.Empty;
    public string Key2 { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}
