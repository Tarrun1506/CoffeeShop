using CoffeeShopApp.Enums;

namespace CoffeeShopApp.Models;

public sealed class CoffeeShopSnapshot
{
    public int TotalOrders { get; init; }
    public int PlacedOrders { get; init; }
    public int PreparingOrders { get; init; }
    public int ReadyOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int CancelledOrders { get; init; }
    public IReadOnlyList<CoffeeMachine> Machines { get; init; } = [];
    public IReadOnlyList<Order> ActiveOrders { get; init; } = [];
    public Order? LatestReadyOrder { get; init; }
    public decimal CompletedRevenue { get; init; }
}
