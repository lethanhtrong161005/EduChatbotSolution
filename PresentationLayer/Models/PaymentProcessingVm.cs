namespace Presentation.Models;

public class PaymentProcessingVm
{
    public Guid Id { get; set; }

    public string TransactionCode { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    public string OptionName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
