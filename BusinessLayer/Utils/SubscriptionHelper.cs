using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Entities;

namespace Business.Utils;

public static class SubscriptionHelper
{
    public static async Task<UserSubscription?> GetSubscriptionOfUserAsync(IUnitOfWork unitOfWork, Guid userId, CancellationToken cxlTkn = default)
    {
        var activeSubs = await unitOfWork.UserSubscriptions.GetAsync(filter: e => e.UserId == userId && e.Status == SubscriptionStatus.Active,
                                                                     orderBy: e => e.OrderByDescending(e => e.StartDate),
                                                                     includeProperties: [$"{nameof(UserSubscription.SubscriptionPlan.Tier)}"],
                                                                     cancellationToken: cxlTkn);

        if (activeSubs.Count() > 1)
        {
            Console.WriteLine($"Warning: User with ID {userId} has multiple active subscriptions. Returning the highest-tier and most recent one.");
            return activeSubs.Where(e => e.StartDate.Date <= DateTime.Now.Date && e.EndDate.Date >= DateTime.Now.Date)
                             .OrderByDescending(e => e.SubscriptionPlan.Tier)
                             .ThenByDescending(e => e.StartDate)
                             .First();
        }
        return activeSubs.FirstOrDefault();
    }

    public static async Task<PurchaseType> GetPurchaseType(UserSubscription targetSub, UserSubscription? activeSub)
    {
        if (activeSub == null)
            return PurchaseType.New;
        else if (activeSub.SubscriptionPlan.Tier < targetSub.SubscriptionPlan.Tier)
            return PurchaseType.Upgrade;
        else if (activeSub.SubscriptionPlan.Tier > targetSub.SubscriptionPlan.Tier)
            return PurchaseType.Downgrade;
        else
            return PurchaseType.Renewal;
    }

    public static async Task NormalizeSubscriptionScheduleAndCharge(IUnitOfWork unitOfWork, UserSubscription targetSub, CancellationToken cxlTkn = default)
    {
        var otherSubs = await unitOfWork.UserSubscriptions.GetAsync(filter: e => e.UserId == targetSub.UserId
                                                                                 && e.Id != targetSub.Id,
                                                                    orderBy: e => e.OrderByDescending(e => e.SubscriptionPlan.Tier)
                                                                                   .ThenByDescending(e => e.EndDate),
                                                                    includeProperties: [$"{nameof(UserSubscription.SubscriptionPurchase)}"
                                                                                        + $".{nameof(UserSubscription.SubscriptionPurchase.SubscriptionPlanOption)}"
                                                                                        + $".{nameof(UserSubscription.SubscriptionPurchase.SubscriptionPlanOption.SubscriptionPlan)}"],
                                                                    cancellationToken: cxlTkn);

        while (true)
        {
            var overlapSubs = otherSubs.Where(e => e.StartDate < targetSub.EndDate && e.EndDate > targetSub.StartDate);
            if (!overlapSubs.Any())
                return;

            var blockingSub = overlapSubs.First();
            if (blockingSub.SubscriptionPlan.Tier >= targetSub.SubscriptionPlan.Tier)
            {
                var slideTimeSpan = blockingSub.EndDate - targetSub.StartDate;
                targetSub.StartDate = blockingSub.EndDate;
                targetSub.EndDate = targetSub.EndDate.Add(slideTimeSpan);
                continue;
            }
            else
            {
                var segments = GetOverlapTimeSegments(targetSub, overlapSubs);
                var credit = CalculateUpgradeProratedCredit(overlapSubs, segments);
                targetSub.SubscriptionPurchase.ChargedAmount -= credit;
                return;
            }
        }
    }

    public class Point
    {
        public DateTime Time { get; set; }
        public int Type { get; set; }   // 0: Start, 1: End
        public int Tier { get; set; }
    }

    public class TimeSegment
    {
        public TimeSpan TimeSpan { get; set; }
        public int Tier { get; set; }
    }

    public static TimeSegment[] GetOverlapTimeSegments(UserSubscription targetSub, IEnumerable<UserSubscription> overlapSubs)
    {
        var points = overlapSubs
            .Append(targetSub)
            .SelectMany(e => new[]
            {
                    new Point{ Time = e.StartDate, Type = 0, Tier = e.SubscriptionPlan.Tier},
                    new Point{ Time = e.EndDate, Type = 1, Tier = e.SubscriptionPlan.Tier}
            })
            .OrderBy(e => e.Time)
            .ThenByDescending(e => e.Type)  // End < Start
            .ThenByDescending(e => e.Tier)
            .ToArray();

        var startIdx = Array.FindIndex(points, e => e.Time == targetSub.StartDate && e.Type == 0 && e.Tier == targetSub.SubscriptionPlan.Tier);
        var endIdx = Array.FindIndex(points, e => e.Time == targetSub.EndDate && e.Type == 1 && e.Tier == targetSub.SubscriptionPlan.Tier);
        points[startIdx].Tier = points[endIdx].Tier = int.MinValue; // Exclude targetSub itself from tiers calculation

        var segments = new TimeSegment[points.Length - 1];

        var tiers = new List<int>() { int.MinValue };

        for (int i = 0; i < points.Length - 1; i++)
        {
            var point = points[i];
            var nextPoint = points[i + 1];

            if (point.Type == 0) // Start
                tiers.Add(point.Tier);
            else // End
                tiers.Remove(point.Tier);

            segments[i] = new TimeSegment()
            {
                TimeSpan = nextPoint.Time - point.Time,
                Tier = tiers.Max(),
            };
        }

        return [.. segments.Skip(startIdx).Take(endIdx - startIdx)];
    }

    public static decimal CalculateUpgradeProratedCredit(IEnumerable<UserSubscription> overlapSubscriptions, TimeSegment[] overlapTimeSegments)
    {
        var totalCredit = 0m;
        for (int i = 0; i < overlapTimeSegments.Length; i++)
        {
            var segment = overlapTimeSegments[i];
            var sub = overlapSubscriptions.First(e => e.SubscriptionPlan.Tier == segment.Tier); // Tiers are unique
            var hourlyRate = sub.SubscriptionPurchase.ChargedAmount / (sub.SubscriptionPlanOption.DurationDays * 24);
            totalCredit += hourlyRate * (int)Math.Round(segment.TimeSpan.TotalHours);
        }
        return totalCredit;
    }
}
