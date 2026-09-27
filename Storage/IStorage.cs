namespace CoffeeShopApp.Storage;

public interface IStorage
{
    Task<List<T>> LoadAsync<T>(string fileName, CancellationToken cancellationToken = default);
    Task SaveAsync<T>(string fileName, IReadOnlyCollection<T> records, CancellationToken cancellationToken = default);
}
