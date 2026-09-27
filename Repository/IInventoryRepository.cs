using CoffeeShopApp.Models;

namespace CoffeeShopApp.Repository;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ReserveAsync(Coffee coffee, int quantity, CancellationToken ct = default);
    Task ReleaseAsync(Coffee coffee, int quantity, CancellationToken ct = default);
    Task RestockAsync(CancellationToken ct = default);
}
