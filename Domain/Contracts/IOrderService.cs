using Domain.Entities;

namespace Domain.Contracts;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAsync(CancellationToken cxlTkn = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cxlTkn = default);

    Task<Order?> CreateAsync(Order entity, CancellationToken cxlTkn = default);
    Task<Order?> UpdateAsync(Order entity, CancellationToken cxlTkn = default);
    Task<Order?> DeleteAsync(Guid id, CancellationToken cxlTkn = default);
}
