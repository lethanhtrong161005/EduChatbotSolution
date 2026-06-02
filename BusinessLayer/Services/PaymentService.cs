using Business.Utils;
using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using System.Linq.Expressions;

namespace Business.Services;

public class PaymentService(IUnitOfWork unitOfWork) : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Payment>> GetAsync(
        Expression<Func<Payment, bool>>? filter = null,
        Func<IQueryable<Payment>, IOrderedQueryable<Payment>>? orderBy = null,
        string[] includeProperties = null!,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Payments.GetAsync(
            filter: filter,
            orderBy: orderBy,
            includeProperties: includeProperties,
            cancellationToken: cxlTkn);
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.Payments.GetAsync(filter: e => e.Id == id,
                                                    includeProperties: [nameof(Payment.Order)
                                                                       + "."
                                                                       + nameof(Payment.Order.Subscription)
                                                                       + "."
                                                                       + nameof(Payment.Order.Subscription.PlanOption)
                                                                       + "."
                                                                       + nameof(Payment.Order.Subscription.PlanOption.Plan)],
                                                    cancellationToken: cxlTkn))
                                          .FirstOrDefault();
    }

    public async Task<Payment?> CreateAsync(Payment entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Payments.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Payment?> UpdateAsync(Payment entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Payments.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Payment?> DeleteAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Payments.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<Payment> CreatePendingPaymentAsync(Guid orderId, PaymentMethod paymentMethod, string? extTxnCode = null, CancellationToken cxlTkn = default)
    {
        var order = (await _unitOfWork.Orders.GetAsync(filter: e => e.Id == orderId,
                                                      cancellationToken: cxlTkn))
                                             .FirstOrDefault()
                    ?? throw new EntityNotFoundException("No order matched the provided ID.");

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new EntityConstraintException("Only pending orders can be paid for.");
        }

        var pendingPayment = new Payment
        {
            OrderId = order.Id,
            Amount = order.ChargedAmount,
            TransactionCode = await GenerateTransactionCode(order),
            ExternalTransactionCode = extTxnCode,
            PaymentMethod = paymentMethod.ToString(),
            Status = PaymentStatus.Pending,
        };

        var insertedEntity = _unitOfWork.Payments.Insert(pendingPayment);
        await _unitOfWork.SaveAsync(cxlTkn);
        return insertedEntity;
    }

    public async Task<Payment> CompletePaymentAsync(Guid? paymentId = null, string? extTxnCode = null, CancellationToken cxlTkn = default)
    {
        if (paymentId == null && extTxnCode == null)
        {
            throw new ArgumentException("Either payment ID or external transaction code must be provided.");
        }

        Expression<Func<Payment, bool>> filter;
        if (paymentId != null && extTxnCode != null)
            filter = e => e.Id == paymentId && e.ExternalTransactionCode == extTxnCode;
        else if (paymentId != null)
            filter = e => e.Id == paymentId;
        else
            filter = e => e.ExternalTransactionCode == extTxnCode;

        var payment = (await _unitOfWork.Payments.GetAsync(filter: filter,
                                                           includeProperties: [nameof(Payment.Order)
                                                                               + "."
                                                                               + nameof(Payment.Order.Subscription)
                                                                               + "."
                                                                               + nameof(Payment.Order.Subscription.PlanOption)
                                                                               + "."
                                                                               + nameof(Payment.Order.Subscription.PlanOption.Plan)],
                                                           cancellationToken: cxlTkn))
                                                 .FirstOrDefault()
                      ?? throw new EntityNotFoundException("No transaction matched the provided ID and/or transaction code.");

        payment.Status = PaymentStatus.Fulfilled;
        payment.PaidAt = DateTime.UtcNow;

        var order = payment.Order;
        var sub = order.Subscription;

        order.Status = OrderStatus.Completed;

        await SubscriptionHelper.NormalizeSubscriptionScheduleAndCharge(
            _unitOfWork,
            sub,
            recalculateCharge: false,
            updateOtherSubscriptions: true,
            cxlTkn);

        if (sub.StartDate > DateTime.UtcNow)
        {
            sub.Status = SubscriptionStatus.Upcoming;
        }
        else
        {
            //var duration = sub.PlanOption.DurationDays;
            var duration = sub.EndDate - sub.StartDate;
            sub.StartDate = DateTime.UtcNow;
            sub.EndDate = DateTime.UtcNow + duration;
            sub.Status = SubscriptionStatus.Active;
        }

        _unitOfWork.Subscriptions.Update(sub);
        _unitOfWork.Orders.Update(order);
        var updatedEntity = _unitOfWork.Payments.Update(payment);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    private async Task<string> GenerateTransactionCode(Order order)
    {
        var activeSub = await SubscriptionHelper.GetSubscriptionOfUserAsync(_unitOfWork, order.UserId);

        var prefix = await SubscriptionHelper.GetOrderType(order.Subscription, activeSub) switch
        {
            OrderType.New => "N",
            OrderType.Upgrade => "U",
            OrderType.Downgrade => "D",
            OrderType.Renewal => "R",
            _ => throw new InvalidOperationException("Unknown order type."),
        };
        var plan = order.Plan.Name.ToUpper()[..2];
        var duration = order.PlanOption.DurationDays.ToString();
        var timestamp = DateTime.UtcNow.ToString("yyMMdd-HHmmss");
        return $"{prefix}-{plan}{duration}-{timestamp}";
    }
}
