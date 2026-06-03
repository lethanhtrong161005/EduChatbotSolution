using Business.Utils;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;

namespace Business.Services;

public class SubscriptionService(IUnitOfWork unitOfWork) : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Plan>> GetPlansAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Plans.GetAsync(includeProperties: [nameof(Plan.PlanOptions)],
                                                cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<PlanOption>> GetPlanOptionsAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.PlanOptions.GetAsync(cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Subscriptions.GetAsync(cancellationToken: cxlTkn);
    }



    public async Task<Plan?> GetPlanAsync(int id, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Plans.GetByIdAsync(id, cxlTkn);
    }

    public async Task<PlanOption?> GetPlanOptionAsync(int id, CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.PlanOptions.GetAsync(filter: e => e.Id == id && e.IsAvailable,
                                                                   includeProperties: [nameof(PlanOption.Plan)],
                                                                   cancellationToken: cxlTkn))
                                                         .FirstOrDefault();
    }

    public async Task<Subscription?> GetSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Subscriptions.GetByIdAsync(id, cxlTkn);
    }

    public async Task<Subscription?> GetSubscriptionOfUserAsync(Guid userId, CancellationToken cxlTkn = default)
    {
        return await SubscriptionHelper.GetSubscriptionOfUserAsync(_unitOfWork, userId, cxlTkn);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsToPlanAsync(
        int id,
        SubscriptionStatus status = SubscriptionStatus.Active,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Subscriptions.GetAsync(filter: e => e.Plan.Id == id && e.Status == status,
                                                        orderBy: e => e.OrderByDescending(e => e.StartDate),
                                                        cancellationToken: cxlTkn);
    }



    public async Task<Subscription?> CreateSubscriptionAsync(Subscription entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Subscriptions.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Subscription?> UpdateSubscriptionAsync(Subscription entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Subscriptions.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Subscription?> DeleteSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Subscriptions.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }



    public async Task<Plan?> CreatePlanAsync(Plan entity, CancellationToken cxlTkn = default)
    {
        var insertedEntity = _unitOfWork.Plans.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return insertedEntity;
    }

    public async Task<Plan?> UpdatePlanAsync(Plan entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Plans.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Plan?> DeletePlanAsync(int id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Plans.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }



    public async Task<PlanOption?> CreatePlanOptionAsync(PlanOption entity, CancellationToken cxlTkn = default)
    {
        var insertedEntity = _unitOfWork.PlanOptions.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return insertedEntity;
    }

    public async Task<PlanOption?> UpdatePlanOptionAsync(PlanOption entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.PlanOptions.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<PlanOption?> DeletePlanOptionAsync(int id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.PlanOptions.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }



    public async Task<Subscription> SubscribeUserToPlanAsync(
        Guid userId,
        int planOptionId,
        bool createOrder = false,
        CancellationToken cxlTkn = default)
    {
        var planOption = (await _unitOfWork.PlanOptions.GetAsync(filter: e => e.Id == planOptionId && e.IsAvailable,
                                                                 includeProperties: [nameof(PlanOption.Plan)],
                                                                 cancellationToken: cxlTkn))
                                                       .FirstOrDefault()
                         ?? throw new EntityNotFoundException("No available plan option matched the request.");

        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(planOption.DurationDays);

        var newSub = new Subscription
        {
            UserId = userId,
            PlanOptionId = planOption.Id,
            StartDate = startDate,
            EndDate = endDate,
            Status = SubscriptionStatus.PendingPayment,

            PlanOption = planOption,
        };
        newSub = _unitOfWork.Subscriptions.Insert(newSub);  // Get generated ID

        if (createOrder)
        {
            var order = new Order
            {
                SubscriptionId = newSub.Id,
                ChargedAmount = newSub.PlanOption.Price,
                Status = OrderStatus.PendingPayment,

                Subscription = newSub,
            };
            _unitOfWork.Orders.Insert(order);
            newSub.Order = order;
        }

        await SubscriptionHelper.NormalizeSubscriptionScheduleAndCharge(
            _unitOfWork,
            newSub,
            recalculateCharge: true,
            updateOtherSubscriptions: false,
            cxlTkn);

        await _unitOfWork.SaveAsync(cxlTkn);
        return newSub;
    }

    public async Task<Subscription?> ActivateSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(id, cxlTkn);
        if (subscription == null)
            return null;

        subscription.Status = SubscriptionStatus.Active;
        var updatedEntity = _unitOfWork.Subscriptions.Update(subscription);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Subscription?> CancelSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByIdAsync(id, cxlTkn);
        if (subscription == null)
            return null;

        subscription.Status = SubscriptionStatus.Cancelled;
        var updatedEntity = _unitOfWork.Subscriptions.Update(subscription);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }
}
