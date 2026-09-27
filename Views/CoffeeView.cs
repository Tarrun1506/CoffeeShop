using CoffeeShopApp.Constants;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Views;

public sealed class CoffeeView
{
    public void ShowMenu(IReadOnlyList<Coffee> menu)
    {
        ConsoleView.WriteLine("===== COFFEE MENU =====");
        ConsoleView.WriteLine("No. Coffee          Price    Prep    Availability");
        foreach (Coffee coffee in menu.OrderBy(c => c.Type))
            ConsoleView.WriteLine($"{(int)coffee.Type,-4} {coffee.Name,-15} ₹{coffee.Price,6:0.00}  {coffee.PreparationSeconds,3}s     {(coffee.IsAvailable ? "Available" : "Unavailable")}");
    }

    public (CoffeeType Type, int Quantity) ReadOrder(IReadOnlyList<Coffee> menu)
    {
        ShowMenu(menu);
        CoffeeType type = ConsoleInput.ReadEnum<CoffeeType>("Coffee number: ");
        if (!menu.Any(c => c.Type == type)) throw new InvalidOperationException("That coffee is not on the menu.");
        int quantity = ConsoleInput.ReadIntInRange($"Quantity (1-{Configurables.MaximumOrderQuantity}): ", 1, Configurables.MaximumOrderQuantity);
        return (type, quantity);
    }

    public void ShowOrders(IEnumerable<Order> orders)
    {
        List<Order> rows = orders.ToList();
        ConsoleView.WriteLine("Order       Coffee           Qty      Total      Status       Created");
        ConsoleView.WriteLine("--------------------------------------------------------------------------");
        foreach (Order order in rows) ConsoleView.WriteLine($"ORD{order.Id,-8} {order.CoffeeName,-16} {order.Quantity,3}   ₹{order.Total,8:0.00}  {order.Status,-11} {order.CreatedAt:g} {(order.Status == OrderStatus.Preparing ? $"({order.UnitsPrepared}/{order.Quantity} servings ready)" : "")}");
        if (rows.Count == 0) ConsoleView.WriteLine("No orders found.");
    }
}
