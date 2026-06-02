using Domain.Common;
using Domain.Entities;

namespace Domain.Contracts;

public interface ISubscriptionService
{
    Task<IEnumerable<Plan>> GetPlansAsync(CancellationToken cxlTkn = default);
    Task<IEnumerable<PlanOption>> GetPlanOptionsAsync(CancellationToken cxlTkn = default);
    Task<IEnumerable<Subscription>> GetSubscriptionsAsync(CancellationToken cxlTkn = default);

    Task<Plan?> GetPlanAsync(int id, CancellationToken cxlTkn = default);
    Task<PlanOption?> GetPlanOptionAsync(int id, CancellationToken cxlTkn = default);
    Task<Subscription?> GetSubscriptionAsync(Guid id, CancellationToken cxlTkn = default);
    Task<Subscription?> GetSubscriptionOfUserAsync(Guid userId, CancellationToken cxlTkn = default);
    Task<IEnumerable<Subscription>> GetSubscriptionsToPlanAsync(
        int userId,
        SubscriptionStatus status = SubscriptionStatus.Active,
        CancellationToken cxlTkn = default);

    Task<Subscription?> CreateSubscriptionAsync(Subscription entity, CancellationToken cxlTkn = default);
    Task<Subscription?> UpdateSubscriptionAsync(Subscription entity, CancellationToken cxlTkn = default);
    Task<Subscription?> DeleteSubscriptionAsync(Guid id, CancellationToken cxlTkn = default);

    Task<Plan?> CreatePlanAsync(Plan entity, CancellationToken cxlTkn = default);
    Task<Plan?> UpdatePlanAsync(Plan entity, CancellationToken cxlTkn = default);
    Task<Plan?> DeletePlanAsync(int id, CancellationToken cxlTkn = default);

    Task<PlanOption?> CreatePlanOptionAsync(PlanOption entity, CancellationToken cxlTkn = default);
    Task<PlanOption?> UpdatePlanOptionAsync(PlanOption entity, CancellationToken cxlTkn = default);
    Task<PlanOption?> DeletePlanOptionAsync(int id, CancellationToken cxlTkn = default);

    Task<Subscription> SubscribeUserToPlanAsync(
        Guid userId,
        int planOptionId,
        bool createOrder = true,
        CancellationToken cxlTkn = default);
}
