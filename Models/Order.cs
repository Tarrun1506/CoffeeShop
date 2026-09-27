using CoffeeShopApp.Enums;

namespace CoffeeShopApp.Models;

public sealed class Order
{
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CoffeeType CoffeeType { get; set; }
    public string CoffeeName { get; set; } = string.Empty;
    public NotificationMethod NotificationMethod { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => UnitPrice * Quantity;
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReadyAt { get; set; }
    public int UnitsPrepared { get; set; }
    public int PreparationFailures { get; set; }
}
