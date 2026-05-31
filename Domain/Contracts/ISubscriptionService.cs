using Domain.Common;
using Domain.Entities;

namespace Domain.Contracts;

public interface ISubscriptionService
{
    Task<IEnumerable<SubscriptionPlan>> GetPlansAsync(CancellationToken cxlTkn = default);
    Task<IEnumerable<UserSubscription>> GetSubscriptionsAsync(CancellationToken cxlTkn = default);

    Task<SubscriptionPlan?> GetPlanAsync(int id, CancellationToken cxlTkn = default);
    Task<UserSubscription?> GetSubscriptionAsync(Guid id, CancellationToken cxlTkn = default);
    Task<UserSubscription?> GetSubscriptionOfUserAsync(Guid userId, CancellationToken cxlTkn = default);
    Task<IEnumerable<UserSubscription>> GetSubscriptionsToPlanAsync(
        int userId,
        SubscriptionStatus status = SubscriptionStatus.Active,
        CancellationToken cxlTkn = default);

    Task<UserSubscription?> CreateSubscriptionAsync(UserSubscription entity, CancellationToken cxlTkn = default);
    Task<UserSubscription?> UpdateSubscriptionAsync(UserSubscription entity, CancellationToken cxlTkn = default);
    Task<UserSubscription?> DeleteSubscriptionAsync(Guid id, CancellationToken cxlTkn = default);

    Task<SubscriptionPlan?> CreatePlanAsync(SubscriptionPlan entity, CancellationToken cxlTkn = default);
    Task<SubscriptionPlan?> UpdatePlanAsync(SubscriptionPlan entity, CancellationToken cxlTkn = default);
    Task<SubscriptionPlan?> DeletePlanAsync(int id, CancellationToken cxlTkn = default);

    Task<UserSubscription?> SubscribeUserToPlanAsync(
        Guid userId,
        SubscriptionPlanOption subPlanOptions,
        CancellationToken cxlTkn = default);
}
