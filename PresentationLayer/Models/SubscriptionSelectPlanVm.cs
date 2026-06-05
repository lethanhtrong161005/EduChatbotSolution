using Domain.Common;
using Domain.Entities;

namespace Presentation.Models;

public class SubscriptionSelectPlanVm
{
    public IEnumerable<PlanCardVm> Plans { get; set; } = [];

    public CurrentSubscriptionVm? CurrentSubscription { get; set; }
}

public class PlanCardVm
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Tier { get; set; }

    public string? Description { get; set; }

    public int DailyMessageQuota { get; set; }

    public int ChatSessionLimit { get; set; }

    public int DailyFileUploadQuota { get; set; }

    public int FileLibraryLimit { get; set; }

    public bool AllowAdvancedModels { get; set; }

    public bool IsFeatured { get; set; }

    public ICollection<PlanOptionCardVm> Options { get; set; } = [];

    public string DisplayDailyMessageQuota => DailyMessageQuota == AppConstants.UnlimitedQuota ? "Unlimited" : DailyMessageQuota.ToString("N0");
    public string DisplayChatSessionLimit => ChatSessionLimit == AppConstants.UnlimitedQuota ? "Unlimited" : ChatSessionLimit.ToString("N0");
    public string DisplayDailyFileUploadQuota => DailyFileUploadQuota == AppConstants.UnlimitedQuota ? "Unlimited" : DailyFileUploadQuota.ToString("N0");
    public string DisplayFileLibraryLimit => FileLibraryLimit == AppConstants.UnlimitedQuota ? "Unlimited" : FileLibraryLimit.ToString("N0");
    public string DisplayAllowAdvancedModels => AllowAdvancedModels ? "Included" : "Not Available";
}

public class CurrentSubscriptionVm
{
    public string PlanName { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
