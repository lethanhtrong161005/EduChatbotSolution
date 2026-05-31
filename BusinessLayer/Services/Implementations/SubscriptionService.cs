using Business.Utils;
using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Services.Implementations;

public class SubscriptionService(IUnitOfWork unitOfWork) : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<SubscriptionPlan>> GetPlansAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.SubscriptionPlans.GetAsync(cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<UserSubscription>> GetSubscriptionsAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.UserSubscriptions.GetAsync(cancellationToken: cxlTkn);
    }

    public async Task<SubscriptionPlan?> GetPlanAsync(int id, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.SubscriptionPlans.GetByIdAsync(id, cxlTkn);
    }

    public async Task<UserSubscription?> GetSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.UserSubscriptions.GetByIdAsync(id, cxlTkn);
    }

    public async Task<UserSubscription?> GetSubscriptionOfUserAsync(Guid userId, CancellationToken cxlTkn = default)
    {
        return await SubscriptionHelper.GetSubscriptionOfUserAsync(_unitOfWork, userId, cxlTkn);
    }

    public async Task<IEnumerable<UserSubscription>> GetSubscriptionsToPlanAsync(
        int id,
        SubscriptionStatus status = SubscriptionStatus.Active,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.UserSubscriptions.GetAsync(filter: e => e.SubscriptionPlan.Id == id && e.Status == status,
                                                            orderBy: e => e.OrderByDescending(e => e.StartDate),
                                                            cancellationToken: cxlTkn);
    }

    public async Task<UserSubscription?> CreateSubscriptionAsync(UserSubscription entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.UserSubscriptions.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<UserSubscription?> UpdateSubscriptionAsync(UserSubscription entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.UserSubscriptions.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<UserSubscription?> DeleteSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.UserSubscriptions.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<SubscriptionPlan?> CreatePlanAsync(SubscriptionPlan entity, CancellationToken cxlTkn = default)
    {
        var insertedEntity = _unitOfWork.SubscriptionPlans.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return insertedEntity;
    }

    public async Task<SubscriptionPlan?> UpdatePlanAsync(SubscriptionPlan entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.SubscriptionPlans.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<SubscriptionPlan?> DeletePlanAsync(int id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.SubscriptionPlans.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<UserSubscription?> SubscribeUserToPlanAsync(Guid userId, SubscriptionPlanOption subPlanOptions, CancellationToken cxlTkn = default)
    {
        var newSub = new UserSubscription
        {
            UserId = userId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(subPlanOptions.DurationDays),
            Status = SubscriptionStatus.PendingPayment,
        };

        var purchase = new SubscriptionPurchase
        {
            SubscriptionPlanOptionId = subPlanOptions.Id,
            ChargedAmount = subPlanOptions.Price,
            PurchasedAt = DateTime.Now
        };

        // Handle potential overlaps with existing subscriptions

        var insertedEntity = _unitOfWork.UserSubscriptions.Insert(newSub);
        await _unitOfWork.SaveAsync(cxlTkn);
        return insertedEntity;
    }

    public async Task<UserSubscription?> ActivateSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var subscription = await _unitOfWork.UserSubscriptions.GetByIdAsync(id, cxlTkn);
        if (subscription == null)
            return null;

        subscription.Status = SubscriptionStatus.Active;
        var updatedEntity = _unitOfWork.UserSubscriptions.Update(subscription);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<UserSubscription?> CancelSubscriptionAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var subscription = await _unitOfWork.UserSubscriptions.GetByIdAsync(id, cxlTkn);
        if (subscription == null)
            return null;

        subscription.Status = SubscriptionStatus.Cancelled;
        var updatedEntity = _unitOfWork.UserSubscriptions.Update(subscription);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }
}
