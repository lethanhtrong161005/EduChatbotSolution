using Domain.Entities;

namespace Domain.Contracts;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> CreateAsync(Order entity, CancellationToken cancellationToken = default);
    Task<Order?> UpdateAsync(Order entity, CancellationToken cancellationToken = default);
    Task<Order?> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
