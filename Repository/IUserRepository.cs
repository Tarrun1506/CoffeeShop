using CoffeeShopApp.Models;

namespace CoffeeShopApp.Repository;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
