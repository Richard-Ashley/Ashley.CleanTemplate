using Domain.Entities;

namespace Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
