using CoffeeShopApp.Constants;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Views;

public sealed class AdminView
{
    public void ShowMenu() => ConsoleView.WriteLine("===== COFFEE SHOP ADMIN =====\n1. View all orders\n2. View preparing orders\n3. View available machines\n4. View completed orders\n5. Watch preparing orders\n6. View inventory\n7. Restock inventory\n8. View today's orders\n9. View coffee menu\n0. Back");

    public void ShowMachines(IEnumerable<CoffeeMachine> machines)
    {
        ConsoleView.WriteLine("Machine    Status       Order");
        foreach (CoffeeMachine machine in machines.OrderBy(m => m.Id)) ConsoleView.WriteLine($"{machine.Id,-10} {machine.Status,-12} {(machine.OrderId.HasValue ? $"ORD{machine.OrderId}" : "-")}");
    }

    public void ShowInventory(IEnumerable<InventoryItem> items)
    {
        ConsoleView.WriteLine("Inventory (available / capacity)");
        foreach (InventoryItem item in items.OrderBy(i => i.Name)) ConsoleView.WriteLine($"{item.Name,-18} {item.Quantity,5} / {item.MaxQuantity}");
    }

    public async Task WatchPreparingOrdersAsync(Func<CancellationToken, Task<IReadOnlyList<Order>>> load, CoffeeView coffeeView, Action clearContent, CancellationToken ct)
    {
        if (Console.IsInputRedirected) { ConsoleView.Error("Live monitoring needs an interactive console."); return; }
        ConsoleView.WriteLine("Preparing orders refresh every second. Press Q to return.");
        using PeriodicTimer timer = new(Configurables.DashboardRefreshInterval);
        do
        {
            clearContent();
            coffeeView.ShowOrders(await load(ct));
            for (int tick = 0; tick < 10; tick++)
            {
                ct.ThrowIfCancellationRequested();
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) return;
                await timer.WaitForNextTickAsync(ct);
            }
            ConsoleView.WriteLine("--- refresh ---");
        } while (!ct.IsCancellationRequested);
    }
}
