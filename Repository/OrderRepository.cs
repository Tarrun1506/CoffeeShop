using CoffeeShopApp.Constants;
using CoffeeShopApp.Models;
using CoffeeShopApp.Storage;

namespace CoffeeShopApp.Repository;

public sealed class OrderRepository(IStorage storage) : IOrderRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) => await ReadAsync(ct);
    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) => (await ReadAsync(ct)).Where(o => o.CustomerId == customerId).ToList();
    public async Task<Order?> GetAsync(int id, CancellationToken ct = default) => (await ReadAsync(ct)).FirstOrDefault(o => o.Id == id);
    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<Order> orders = await storage.LoadAsync<Order>(Path.GetFileName(Configurables.OrdersFile), ct);
            order.Id = orders.Count == 0 ? 1001 : orders.Max(o => o.Id) + 1;
            orders.Add(order); await storage.SaveAsync(Path.GetFileName(Configurables.OrdersFile), orders, ct); return order;
        }
        finally { _gate.Release(); }
    }
    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<Order> orders = await storage.LoadAsync<Order>(Path.GetFileName(Configurables.OrdersFile), ct);
            int index = orders.FindIndex(o => o.Id == order.Id);
            if (index < 0) throw new KeyNotFoundException($"Order {order.Id} was not found.");
            orders[index] = order; await storage.SaveAsync(Path.GetFileName(Configurables.OrdersFile), orders, ct);
        }
        finally { _gate.Release(); }
    }
    private async Task<List<Order>> ReadAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { return await storage.LoadAsync<Order>(Path.GetFileName(Configurables.OrdersFile), ct); }
        finally { _gate.Release(); }
    }
}
