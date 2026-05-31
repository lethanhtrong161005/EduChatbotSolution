using Business.Utils;
using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;

namespace Business.Services.Implementations;

public class PaymentService(
    IUnitOfWork unitOfWork)
    : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<PaymentTransaction>> GetAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.PaymentTransactions.GetAsync(cancellationToken: cxlTkn);
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.PaymentTransactions.GetByIdAsync(id, cxlTkn);
    }

    public async Task<PaymentTransaction?> CreateAsync(PaymentTransaction entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.PaymentTransactions.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<PaymentTransaction?> UpdateAsync(PaymentTransaction entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.PaymentTransactions.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<PaymentTransaction?> DeleteAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.PaymentTransactions.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<PaymentTransaction?> CreatePendingPaymentAsync(SubscriptionPurchase subscriptionPurchase, PaymentMethod paymentMethod, CancellationToken cxlTkn = default)
    {
        if (subscriptionPurchase.Id == Guid.Empty)
        {
            throw new ArgumentException("User subscription must have a valid ID.", nameof(subscriptionPurchase));
        }

        var transactionCode = await GenerateTransactionCode(subscriptionPurchase);

        var pendingPayment = new PaymentTransaction
        {
            SubscriptionPurchaseId = subscriptionPurchase.Id,
            TransactionCode = transactionCode,
            Amount = subscriptionPurchase.SubscriptionPlanOption.Price,
            PaymentMethod = paymentMethod.ToString(),
            PaymentStatus = PaymentStatus.Pending,
        };

        var insertedEntity = _unitOfWork.PaymentTransactions.Insert(pendingPayment);
        await _unitOfWork.SaveAsync(cxlTkn);
        return insertedEntity;
    }

    public async Task<PaymentTransaction?> CompletePaymentAsync(Guid paymentId, CancellationToken cxlTkn = default)
    {
        var payment = await _unitOfWork.PaymentTransactions.GetByIdAsync(paymentId, cxlTkn)
                      ?? throw new EntityNotFoundException("No transaction matched the provided ID.");

        payment.PaymentStatus = PaymentStatus.Fulfilled;
        payment.PaidAt = DateTime.Now;
        var updatedEntity = _unitOfWork.PaymentTransactions.Update(payment);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    private async Task<string> GenerateTransactionCode(SubscriptionPurchase subscriptionPurchase)
    {
        var activeSub = await SubscriptionHelper.GetSubscriptionOfUserAsync(_unitOfWork, subscriptionPurchase.UserId);

        var prefix = await SubscriptionHelper.GetPurchaseType(subscriptionPurchase.UserSubscription, activeSub) switch
        {
            PurchaseType.New => "N",
            PurchaseType.Upgrade => "U",
            PurchaseType.Downgrade => "D",
            PurchaseType.Renewal => "R",
            _ => throw new InvalidOperationException("Unknown purchase type."),
        };
        var plan = subscriptionPurchase.SubscriptionPlanOption.SubscriptionPlan.Name.ToUpper()[0..1];
        var timestamp = DateTime.Now.ToString("yyMMdd-HHmmss");
        return $"{prefix}{plan}-{timestamp}";
    }
}
