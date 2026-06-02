using Domain.Common;

namespace Presentation.Models;

public class PaymentSelectMethodVm
{
    public List<PaymentMethodVm> PaymentMethods { get; set; } = [];

    public OrderCheckoutVm PendingOrder { get; set; } = null!;

    public PaymentMethod SelectedMethod { get; set; }
}

public class OrderCheckoutVm
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string OptionName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Total { get; set; }
}

public class PaymentMethodVm
{
    public string Name { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string ImageSource { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
