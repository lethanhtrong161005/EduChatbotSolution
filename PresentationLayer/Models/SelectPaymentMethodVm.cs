using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models;

public class SelectPaymentMethodVm
{
    [FromQuery(Name = "option-id")]
    public int SubscriptionPlanId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
}
