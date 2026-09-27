using CoffeeShopApp.Enums;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Repository;

public interface ICoffeeRepository
{
    Task<IReadOnlyList<Coffee>> GetAllAsync(CancellationToken ct = default);
    Task<Coffee?> GetAsync(CoffeeType type, CancellationToken ct = default);
}
