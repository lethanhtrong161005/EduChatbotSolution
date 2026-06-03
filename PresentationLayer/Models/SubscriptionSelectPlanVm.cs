using Domain.Entities;

namespace Presentation.Models;

public class SubscriptionSelectPlanVm
{
    public IEnumerable<PlanCardVm> Plans { get; set; } = [];

    public CurrentSubscriptionVm? CurrentSubscription { get; set; }
}

public class CurrentSubscriptionVm
{
    public string PlanName { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
