using CoffeeShopApp.Enums;

namespace CoffeeShopApp.Models;

public sealed class Coffee
{
    public CoffeeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SourcingSeconds { get; set; }
    public int PreparationSeconds { get; set; }
    public Dictionary<string, int> Ingredients { get; set; } = new();
    public bool IsAvailable { get; set; } = true;
}
