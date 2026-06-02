using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Services;

public class OrderService(IUnitOfWork unitOfWork) : IOrderService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Order>> GetAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Orders.GetAsync(cancellationToken: cxlTkn);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var order = (await _unitOfWork.Orders.GetAsync(filter: e => e.Id == id,
            includeProperties: [nameof(Order.Subscription)
                                + "."
                                + nameof(Order.Subscription.PlanOption)
                                + "."
                                + nameof(Order.Subscription.PlanOption.Plan)],
            cancellationToken: cxlTkn));

        return await _unitOfWork.Orders.GetByIdAsync(id, cxlTkn);
    }

    public async Task<Order?> CreateAsync(Order entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Orders.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Order?> UpdateAsync(Order entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Orders.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Order?> DeleteAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Orders.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }
}
