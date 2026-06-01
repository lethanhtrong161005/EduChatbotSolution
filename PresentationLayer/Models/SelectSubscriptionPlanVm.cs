using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models;

public class SelectSubscriptionPlanVm
{
    public IEnumerable<SubscriptionPlanCardVm> Plans { get; set; } = [];

    [FromQuery(Name = "order")]
    public SelectPaymentMethodVm Order { get; set; } = null!;
}
