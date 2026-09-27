using CoffeeShopApp.Enums;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Views;

public sealed class DashboardRenderer
{
    private const int DashboardHeight = 11;
    private CoffeeShopSnapshot _snapshot = new();
    private string _userLabel = "Guest";

    public int ContentStartLine => DashboardHeight + 1;

    public void Update(CoffeeShopSnapshot snapshot, string? userLabel = null)
    {
        _snapshot = snapshot;
        if (userLabel is not null) _userLabel = userLabel;
        RenderDashboard();
    }

    public void RenderDashboard()
    {
        lock (ConsoleCoordinator.Gate)
        {
            if (Console.IsOutputRedirected)
            {
                RenderPlain();
                return;
            }
            try
            {
                int oldTop = Console.CursorTop, oldLeft = Console.CursorLeft;
                int width = Math.Max(50, Console.WindowWidth - 1);
                for (int row = 0; row < DashboardHeight; row++)
                {
                    Console.SetCursorPosition(0, row);
                    Console.Write(new string(' ', Math.Min(width, Console.WindowWidth)));
                }
                Console.SetCursorPosition(0, 0);
                RenderLines(width);
                Console.SetCursorPosition(Math.Min(oldLeft, Math.Max(0, Console.WindowWidth - 1)), oldTop >= ContentStartLine ? oldTop : ContentStartLine);
            }
            catch (IOException) { RenderPlain(); }
            catch (ArgumentOutOfRangeException) { RenderPlain(); }
        }
    }

    public void ClearContentArea()
    {
        lock (ConsoleCoordinator.Gate)
        {
            if (Console.IsOutputRedirected) return;
            try
            {
                int width = Math.Max(1, Console.WindowWidth - 1);
                for (int row = ContentStartLine; row < Console.WindowHeight; row++)
                {
                    Console.SetCursorPosition(0, row);
                    Console.Write(new string(' ', width));
                }
                Console.SetCursorPosition(0, ContentStartLine);
            }
            catch (IOException) { }
            catch (ArgumentOutOfRangeException) { }
        }
    }

    public void MoveToContentArea()
    {
        if (Console.IsOutputRedirected) { Console.WriteLine(); return; }
        try { Console.SetCursorPosition(0, ContentStartLine); }
        catch (IOException) { Console.WriteLine(); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine(); }
    }

    private void RenderLines(int width)
    {
        string border = "+" + new string('=', Math.Max(1, width - 2)) + "+";
        WriteRaw(border, ConsoleColor.Cyan);
        WriteLine(Center("COFFEE SHOP  |  LIVE OPERATIONS DASHBOARD", width));
        WriteLine($"Status: OPEN     Customer: {_userLabel}");
        string machines = string.Join("   ", _snapshot.Machines.OrderBy(m => m.Id).Select(m => $"Machine {m.Id}: {m.Status.ToString().ToUpperInvariant()}" + (m.OrderId.HasValue ? $" (ORD{m.OrderId})" : "")));
        WriteLine(string.IsNullOrEmpty(machines) ? "Machines: loading..." : machines);
        WriteLine($"Orders  |  Waiting: {_snapshot.PlacedOrders}   Preparing: {_snapshot.PreparingOrders}   Ready: {_snapshot.ReadyOrders}   Completed: {_snapshot.CompletedOrders}   Total: {_snapshot.TotalOrders}");
        string active = _snapshot.ActiveOrders.Count == 0 ? "No active orders" : string.Join("  |  ", _snapshot.ActiveOrders.Select(o => $"ORD{o.Id} {o.CoffeeName} ×{o.Quantity} [{o.Status}{(o.Status == OrderStatus.Preparing ? $" {o.UnitsPrepared}/{o.Quantity}" : "")}]"));
        WriteLine("Queue: " + active);
        string ready = _snapshot.LatestReadyOrder is null ? "Last ready: none yet" : $"Last ready: ORD{_snapshot.LatestReadyOrder.Id} {_snapshot.LatestReadyOrder.CoffeeName}";
        WriteLine($"{ready}     Completed sales: ₹{_snapshot.CompletedRevenue:0.00}");
        WriteLine($"Updated: {DateTime.Now:HH:mm:ss}");
        WriteRaw(border, ConsoleColor.Cyan);
    }

    private void RenderPlain()
    {
        Console.WriteLine("\n+================ COFFEE SHOP DASHBOARD ================+");
        Console.WriteLine($"Status: OPEN   Customer: {_userLabel}   Machines: {string.Join(" | ", _snapshot.Machines.OrderBy(m => m.Id).Select(m => $"{m.Id}:{m.Status}"))}");
        Console.WriteLine($"Orders: Waiting {_snapshot.PlacedOrders} | Preparing {_snapshot.PreparingOrders} | Ready {_snapshot.ReadyOrders} | Completed {_snapshot.CompletedOrders}");
        Console.WriteLine("+=======================================================+");
    }

    private static void WriteRaw(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color; Console.WriteLine(text); Console.ResetColor();
    }
    private static void WriteLine(string text)
    {
        int width = Console.IsOutputRedirected ? 100 : Math.Max(50, Console.WindowWidth - 1);
        string fitted = Fit(text, width - 4);
        Console.WriteLine("| " + fitted.PadRight(width - 4) + " |");
    }
    private static string Center(string text, int width) => text.Length >= width - 4 ? Fit(text, width - 4) : text.PadLeft((width - 4 + text.Length) / 2).PadRight(width - 4);
    private static string Fit(string text, int width) => text.Length <= width ? text : text[..Math.Max(0, width - 3)] + "...";
}
