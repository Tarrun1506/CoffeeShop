using CoffeeShopApp.Constants;
using CoffeeShopApp.Models;
using CoffeeShopApp.Storage;

namespace CoffeeShopApp.Repository;

public sealed class InventoryRepository(IStorage storage) : IInventoryRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly InventoryItem[] Defaults =
    [new() { Name = "Coffee beans", Quantity = 100, MaxQuantity = 100 }, new() { Name = "Milk", Quantity = 100, MaxQuantity = 100 }, new() { Name = "Water", Quantity = 150, MaxQuantity = 150 }, new() { Name = "Chocolate", Quantity = 50, MaxQuantity = 50 }];

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try { return await LoadOrSeedAsync(ct); }
        finally { _gate.Release(); }
    }
    public async Task<bool> ReserveAsync(Coffee coffee, int quantity, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<InventoryItem> items = await LoadOrSeedAsync(ct);
            foreach ((string name, int amount) in coffee.Ingredients)
                if (items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is not { } item || item.Quantity < amount * quantity) return false;
            foreach ((string name, int amount) in coffee.Ingredients)
                items.First(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Quantity -= amount * quantity;
            await SaveItemsAsync(items, ct); return true;
        }
        finally { _gate.Release(); }
    }
    public async Task ReleaseAsync(Coffee coffee, int quantity, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<InventoryItem> items = await LoadOrSeedAsync(ct);
            foreach ((string name, int amount) in coffee.Ingredients)
            {
                InventoryItem? item = items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (item is not null) item.Quantity = (int)Math.Min(item.MaxQuantity, (long)item.Quantity + (long)amount * quantity);
            }
            await SaveItemsAsync(items, ct);
        }
        finally { _gate.Release(); }
    }
    public async Task RestockAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try { List<InventoryItem> items = await LoadOrSeedAsync(ct); foreach (InventoryItem item in items) item.Quantity = item.MaxQuantity; await SaveItemsAsync(items, ct); }
        finally { _gate.Release(); }
    }
    private async Task<List<InventoryItem>> LoadOrSeedAsync(CancellationToken ct)
    {
        List<InventoryItem> items = await storage.LoadAsync<InventoryItem>(Path.GetFileName(Configurables.InventoryFile), ct);
        if (items.Count == 0) { items = Defaults.Select(i => new InventoryItem { Name = i.Name, Quantity = i.Quantity, MaxQuantity = i.MaxQuantity }).ToList(); await SaveItemsAsync(items, ct); }
        else
        {
            bool migrated = false;
            foreach (InventoryItem item in items)
                if (item.MaxQuantity == 0) { item.MaxQuantity = item.Quantity; migrated = true; }
            if (migrated) await SaveItemsAsync(items, ct);
        }
        return items;
    }
    private Task SaveItemsAsync(List<InventoryItem> items, CancellationToken ct) => storage.SaveAsync(Path.GetFileName(Configurables.InventoryFile), items, ct);
}
