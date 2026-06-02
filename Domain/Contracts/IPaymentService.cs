using Domain.Common;
using Domain.Entities;
using System.Linq.Expressions;

namespace Domain.Contracts;

public interface IPaymentService
{
    Task<IEnumerable<Payment>> GetAsync(Expression<Func<Payment, bool>>? filter = null, Func<IQueryable<Payment>, IOrderedQueryable<Payment>>? orderBy = null, string[] includeProperties = null!, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Payment?> CreateAsync(Payment entity, CancellationToken cancellationToken = default);
    Task<Payment?> UpdateAsync(Payment entity, CancellationToken cancellationToken = default);
    Task<Payment?> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Payment> CreatePendingPaymentAsync(Guid orderId, PaymentMethod paymentMethod, string? externalTransactionCode = null, CancellationToken cancellationToken = default);
    Task<Payment> CompletePaymentAsync(Guid? paymentId = null, string? externalTransactionCode = null, CancellationToken cancellationToken = default);
}
