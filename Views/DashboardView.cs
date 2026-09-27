using CoffeeShopApp.Constants;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;
using CoffeeShopApp.Services;

namespace CoffeeShopApp.Views;

public sealed class DashboardView(ICoffeeShopService service, Logger logger) : IAsyncDisposable
{
    private readonly DashboardRenderer _renderer = new();
    private readonly CancellationTokenSource _refreshCts = new();
    private Task? _refreshTask;
    private string _userLabel = "Guest";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _renderer.Update(await service.GetDashboardSnapshotAsync(ct), _userLabel);
        if (!Console.IsOutputRedirected) _refreshTask = Task.Run(() => RefreshLoopAsync(_refreshCts.Token));
    }

    public async Task SetUserAsync(string? name, CancellationToken ct = default)
    {
        _userLabel = string.IsNullOrWhiteSpace(name) ? "Guest" : name;
        _renderer.Update(await service.GetDashboardSnapshotAsync(ct), _userLabel);
    }

    public void ShowMainMenu()
    {
        _renderer.ClearContentArea(); _renderer.MoveToContentArea();
        ConsoleView.WriteLine("===== COFFEE SHOP =====");
        ConsoleView.WriteLine("1. Register"); ConsoleView.WriteLine("2. Login"); ConsoleView.WriteLine("3. Admin console"); ConsoleView.WriteLine("0. Exit");
    }

    public void ClearContentArea() { _renderer.ClearContentArea(); _renderer.MoveToContentArea(); }
    public void DisplayMessage(string message, bool isError = false)
    {
        ClearContentArea();
        if (isError) ConsoleView.Error(message); else ConsoleView.WriteLine(message);
        ConsoleView.WriteLine("\nPress Enter to continue...");
        Console.ReadLine();
    }

    public void WaitForEnter()
    {
        ConsoleView.WriteLine("\nPress Enter to continue...");
        Console.ReadLine();
    }

    private async Task RefreshLoopAsync(CancellationToken ct)
    {
        using PeriodicTimer timer = new(Configurables.DashboardRefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try { _renderer.Update(await service.GetDashboardSnapshotAsync(ct), _userLabel); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError("DashboardRefresh", ex.ToString()); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError("DashboardRefresh", ex.ToString()); }
    }

    public async ValueTask DisposeAsync()
    {
        _refreshCts.Cancel();
        if (_refreshTask is not null)
        {
            try { await _refreshTask; } catch (OperationCanceledException) { }
        }
        _refreshCts.Dispose();
    }
}
