using CoffeeShopApp.Models;

namespace CoffeeShopApp.Events;

public sealed class OrderChangedEventArgs(Order order) : EventArgs
{
    public Order Order { get; } = order;
}
