namespace CoffeeShopApp.Models;

public sealed class InventoryItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MaxQuantity { get; set; }
}
