using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeShopApp.Constants;
using CoffeeShopApp.Exceptions;

namespace CoffeeShopApp.Storage;

public sealed class JsonStorage : IStorage
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public JsonStorage() => Directory.CreateDirectory(Configurables.DataDirectory);

    public async Task<List<T>> LoadAsync<T>(string fileName, CancellationToken cancellationToken = default)
    {
        string path = Resolve(fileName);
        SemaphoreSlim gate = FileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path)) return [];
            await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<List<T>>(stream, Options, cancellationToken) ?? [];
        }
        catch (JsonException ex) { throw new StorageException(ErrorMessages.InvalidData, ex); }
        catch (IOException ex) { throw new StorageException(ErrorMessages.FileReadFailed, ex); }
        catch (UnauthorizedAccessException ex) { throw new StorageException(ErrorMessages.FileReadFailed, ex); }
        finally { gate.Release(); }
    }

    public async Task SaveAsync<T>(string fileName, IReadOnlyCollection<T> records, CancellationToken cancellationToken = default)
    {
        string path = Resolve(fileName);
        SemaphoreSlim gate = FileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            string temp = path + ".tmp";
            try
            {
                await using (FileStream stream = new(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                    await JsonSerializer.SerializeAsync(stream, records, Options, cancellationToken);
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        catch (IOException ex) { throw new StorageException(ErrorMessages.FileWriteFailed, ex); }
        catch (UnauthorizedAccessException ex) { throw new StorageException(ErrorMessages.FileWriteFailed, ex); }
        finally { gate.Release(); }
    }

    private string Resolve(string fileName)
    {
        if (Path.GetFileName(fileName) != fileName) throw new ArgumentException("Storage accepts file names only.", nameof(fileName));
        return Path.Combine(Configurables.DataDirectory, fileName);
    }
}
