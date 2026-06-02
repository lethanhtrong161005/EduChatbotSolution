using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Entities;

namespace Business.Utils;

public static class SubscriptionHelper
{
    public static async Task<Subscription?> GetSubscriptionOfUserAsync(IUnitOfWork unitOfWork, Guid userId, CancellationToken cxlTkn = default)
    {
        var activeSubs = await unitOfWork.Subscriptions.GetAsync(filter: e => e.UserId == userId && e.Status == SubscriptionStatus.Active,
                                                                 orderBy: e => e.OrderByDescending(e => e.StartDate),
                                                                 includeProperties: [nameof(Subscription.PlanOption) + "." + nameof(Subscription.Plan)],
                                                                 cancellationToken: cxlTkn);

        if (activeSubs.Count() > 1)
        {
            Console.WriteLine($"Warning: User with ID {userId} has multiple active subscriptions. Returning the highest-tier and most recent one.");
            return activeSubs.Where(e => e.StartDate.Date <= DateTime.Now.Date && e.EndDate.Date >= DateTime.Now.Date)
                             .OrderByDescending(e => e.Plan.Tier)
                             .ThenByDescending(e => e.StartDate)
                             .First();
        }
        return activeSubs.FirstOrDefault();
    }

    public static async Task<OrderType> GetOrderType(Subscription targetSub, Subscription? activeSub)
    {
        if (activeSub == null)
            return OrderType.New;
        else if (activeSub.Plan.Tier < targetSub.Plan.Tier)
            return OrderType.Upgrade;
        else if (activeSub.Plan.Tier > targetSub.Plan.Tier)
            return OrderType.Downgrade;
        else
            return OrderType.Renewal;
    }

    public static async Task NormalizeSubscriptionScheduleAndCharge(
        IUnitOfWork unitOfWork,
        Subscription targetSub,
        bool recalculateCharge = true,
        bool updateOtherSubscriptions = false,
        CancellationToken cxlTkn = default)
    {
        var otherSubs = await unitOfWork.Subscriptions.GetAsync(filter: e => e.UserId == targetSub.UserId
                                                                             && e.Id != targetSub.Id
                                                                             && e.Status != SubscriptionStatus.PendingPayment
                                                                             && e.Status != SubscriptionStatus.Cancelled
                                                                             && e.Status != SubscriptionStatus.Expired,
                                                                orderBy: e => e.OrderByDescending(e => e.PlanOption.Plan.Tier)
                                                                               .ThenByDescending(e => e.EndDate),
                                                                includeProperties:
                                                                [
                                                                    nameof(Subscription.PlanOption) + "." + nameof(Subscription.PlanOption.Plan),
                                                                    nameof(Subscription.Order),
                                                                ],
                                                                cancellationToken: cxlTkn);

        while (true)
        {
            var overlapSubs = otherSubs.Where(e => e.StartDate < targetSub.EndDate && e.EndDate > targetSub.StartDate);
            if (!overlapSubs.Any())
                return;

            var blockingSub = overlapSubs.First();
            if (blockingSub.Plan.Tier >= targetSub.Plan.Tier)
            {
                var slideTimeSpan = blockingSub.EndDate - targetSub.StartDate;
                targetSub.StartDate = blockingSub.EndDate;
                targetSub.EndDate = targetSub.EndDate + slideTimeSpan;
                continue;
            }
            else
            {
                if (!updateOtherSubscriptions
                    && (!recalculateCharge || targetSub.Order == null))
                    return;
                var segments = GetOverlapTimeSegments(targetSub, overlapSubs);
                if (recalculateCharge && targetSub.Order != null)
                {
                    var credit = CalculateUpgradeProratedCredit(overlapSubs, segments);
                    targetSub.Order.ChargedAmount -= credit;
                }
                if (updateOtherSubscriptions)
                {
                    foreach (var sub in overlapSubs)
                    {
                        if (sub.Status is SubscriptionStatus.Upcoming
                                       or SubscriptionStatus.Active
                                       or SubscriptionStatus.Promo)
                        {
                            sub.Status = SubscriptionStatus.Superseded;
                        }
                    }
                }
                return;
            }
        }
    }

    public class Point
    {
        public DateTime Time { get; set; }
        public int Type { get; set; }   // 0: Start, 1: End
        public int Tier { get; set; }
        public Guid SubId { get; set; }
    }

    public class TimeSegment
    {
        public TimeSpan TimeSpan { get; set; }
        public int Tier { get; set; }
        public Guid SubId { get; set; }
    }

    private const int IgnoredTier = int.MinValue;

    public static TimeSegment[] GetOverlapTimeSegments(Subscription targetSub, IEnumerable<Subscription> overlapSubs)
    {
        var points = overlapSubs
            .Append(targetSub)
            .SelectMany(e => new[]
            {
                    new Point{ Time = e.StartDate, Type = 0, Tier = e.Plan.Tier, SubId = e.Id },
                    new Point{ Time = e.EndDate, Type = 1, Tier = e.Plan.Tier, SubId = e.Id },
            })
            .OrderBy(e => e.Time)
            .ThenByDescending(e => e.Type)  // End < Start
            .ThenByDescending(e => e.Tier)
            .ToArray();

        var startIdx = Array.FindIndex(points, e => e.Type == 0 && e.SubId == targetSub.Id);
        var endIdx = Array.FindIndex(points, e => e.Type == 1 && e.SubId == targetSub.Id);
        points[startIdx].Tier = points[endIdx].Tier = IgnoredTier;   // Exclude target sub from tier calculation

        var segments = new TimeSegment[points.Length - 1];

        var tiers = new Dictionary<Guid, int>();

        for (int i = 0; i < points.Length - 1; i++)
        {
            var point = points[i];
            var nextPoint = points[i + 1];

            if (point.Type == 0) // Start
                tiers.Add(point.SubId, point.Tier);
            else // End
                tiers.Remove(point.SubId);

            var topSub = tiers.Count > 0
                ? tiers.MaxBy(e => e.Value)
                : new(Guid.Empty, IgnoredTier);

            segments[i] = new TimeSegment()
            {
                TimeSpan = nextPoint.Time - point.Time,
                Tier = topSub.Value,
                SubId = topSub.Key,
            };
        }

        return [.. segments.Skip(startIdx).Take(endIdx - startIdx)];
    }

    public static decimal CalculateUpgradeProratedCredit(IEnumerable<Subscription> overlapSubscriptions, TimeSegment[] overlapTimeSegments)
    {
        var totalCredit = 0m;
        for (int i = 0; i < overlapTimeSegments.Length; i++)
        {
            var segment = overlapTimeSegments[i];
            if (segment.Tier == IgnoredTier) continue;
            var sub = overlapSubscriptions.First(e => e.Id == segment.SubId);
            var hourlyRate = sub.Order?.ChargedAmount / (sub.PlanOption.DurationDays * 24) ?? 0;
            totalCredit += hourlyRate * (int)Math.Round(segment.TimeSpan.TotalHours);
        }
        return totalCredit;
    }
}
