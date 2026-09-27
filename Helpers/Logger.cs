using CoffeeShopApp.Constants;

namespace CoffeeShopApp.Helpers;

public sealed class Logger
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public void Log(string area, string message) => _ = WriteAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{area}] {message}");
    public void LogError(string area, string message) => _ = WriteAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR:{area}] {message}");

    private static async Task WriteAsync(string line)
    {
        await Gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Configurables.DataDirectory);
            await File.AppendAllTextAsync(Configurables.LogFile, line + Environment.NewLine);
        }
        catch { /* Logging must not bring down order processing. */ }
        finally { Gate.Release(); }
    }
}
