using Domain.Common;
using Domain.Entities;

namespace Domain.Contracts;

public interface IPaymentService
{
    Task<IEnumerable<PaymentTransaction>> GetAsync(CancellationToken cxlTkn = default);
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cxlTkn = default);

    Task<PaymentTransaction?> CreateAsync(PaymentTransaction entity, CancellationToken cxlTkn = default);
    Task<PaymentTransaction?> UpdateAsync(PaymentTransaction entity, CancellationToken cxlTkn = default);
    Task<PaymentTransaction?> DeleteAsync(Guid id, CancellationToken cxlTkn = default);

    Task<PaymentTransaction?> CreatePendingPaymentAsync(SubscriptionPurchase subscriptionPurchase, PaymentMethod paymentMethod, CancellationToken cxlTkn = default);
    Task<PaymentTransaction?> CompletePaymentAsync(Guid paymentId, CancellationToken cxlTkn = default);
}
