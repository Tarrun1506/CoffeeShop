using CoffeeShopApp.Constants;
using CoffeeShopApp.Models;
using CoffeeShopApp.Storage;

namespace CoffeeShopApp.Repository;

public sealed class UserRepository(IStorage storage) : IUserRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) => (await ReadAsync(ct)).FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<User> users = await storage.LoadAsync<User>(Path.GetFileName(Configurables.UsersFile), ct);
            if (users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException(ErrorMessages.DuplicateEmail);
            users.Add(user); await storage.SaveAsync(Path.GetFileName(Configurables.UsersFile), users, ct);
        }
        finally { _gate.Release(); }
    }
    private async Task<List<User>> ReadAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { return await storage.LoadAsync<User>(Path.GetFileName(Configurables.UsersFile), ct); }
        finally { _gate.Release(); }
    }
}
