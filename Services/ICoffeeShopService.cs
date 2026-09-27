using CoffeeShopApp.Events;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Services;

public interface ICoffeeShopService : IAsyncDisposable
{
    event EventHandler<OrderChangedEventArgs>? OrderReady;
    Task InitializeAsync(CancellationToken ct = default);
    Task<User> RegisterAsync(string name, string email, string password, NotificationMethod method, CancellationToken ct = default);
    Task<User?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<Coffee>> GetMenuAsync(CancellationToken ct = default);
    Task<Order> PlaceOrderAsync(User customer, CoffeeType type, int quantity, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(Guid customerId, int orderId, CancellationToken ct = default);
    Task<bool> CompleteOrderAsync(Guid customerId, int orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOrdersAsync(OrderStatus? status = null, bool todayOnly = false, CancellationToken ct = default);
    Task<decimal> GetCustomerSpendingAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<CoffeeMachine>> GetMachinesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(CancellationToken ct = default);
    Task<CoffeeShopSnapshot> GetDashboardSnapshotAsync(CancellationToken ct = default);
    Task RestockAsync(CancellationToken ct = default);
}
