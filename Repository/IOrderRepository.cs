using CoffeeShopApp.Models;

namespace CoffeeShopApp.Repository;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Order?> GetAsync(int id, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
