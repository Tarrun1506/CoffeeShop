using CoffeeShopApp.Models;

namespace CoffeeShopApp.Repository;

public interface IMachineRepository
{
    Task<IReadOnlyList<CoffeeMachine>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(CoffeeMachine machine, CancellationToken ct = default);
}
