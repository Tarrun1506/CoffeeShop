using CoffeeShopApp.Constants;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Models;
using CoffeeShopApp.Storage;

namespace CoffeeShopApp.Repository;

public sealed class CoffeeRepository(IStorage storage) : ICoffeeRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly Coffee[] Defaults =
    [
        new() { Type = CoffeeType.Espresso, Name = "Espresso", Price = 100m, SourcingSeconds = 1, PreparationSeconds = 5, Ingredients = new() { ["Coffee beans"] = 2, ["Water"] = 1 } },
        new() { Type = CoffeeType.Cappuccino, Name = "Cappuccino", Price = 150m, SourcingSeconds = 2, PreparationSeconds = 10, Ingredients = new() { ["Coffee beans"] = 2, ["Milk"] = 2, ["Water"] = 1 } },
        new() { Type = CoffeeType.Latte, Name = "Latte", Price = 160m, SourcingSeconds = 2, PreparationSeconds = 15, Ingredients = new() { ["Coffee beans"] = 2, ["Milk"] = 3, ["Water"] = 1 } },
        new() { Type = CoffeeType.Americano, Name = "Americano", Price = 120m, SourcingSeconds = 1, PreparationSeconds = 7, Ingredients = new() { ["Coffee beans"] = 2, ["Water"] = 2 } },
        new() { Type = CoffeeType.Mocha, Name = "Mocha", Price = 180m, SourcingSeconds = 3, PreparationSeconds = 12, Ingredients = new() { ["Coffee beans"] = 2, ["Milk"] = 2, ["Chocolate"] = 2, ["Water"] = 1 } },
    ];

    public async Task<IReadOnlyList<Coffee>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try { return await LoadOrSeedAsync(ct); }
        finally { _gate.Release(); }
    }
    public async Task<Coffee?> GetAsync(CoffeeType type, CancellationToken ct = default) => (await GetAllAsync(ct)).FirstOrDefault(c => c.Type == type);
    private async Task<List<Coffee>> LoadOrSeedAsync(CancellationToken ct)
    {
        List<Coffee> coffees = await storage.LoadAsync<Coffee>(Path.GetFileName(Configurables.MenuFile), ct);
        if (coffees.Count == 0) { coffees = Defaults.Select(Clone).ToList(); await storage.SaveAsync(Path.GetFileName(Configurables.MenuFile), coffees, ct); }
        return coffees;
    }
    private static Coffee Clone(Coffee coffee) => new() { Type = coffee.Type, Name = coffee.Name, Price = coffee.Price, SourcingSeconds = coffee.SourcingSeconds, PreparationSeconds = coffee.PreparationSeconds, IsAvailable = coffee.IsAvailable, Ingredients = new(coffee.Ingredients) };
}
