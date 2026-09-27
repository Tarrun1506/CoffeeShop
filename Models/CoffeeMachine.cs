using CoffeeShopApp.Enums;

namespace CoffeeShopApp.Models;

public sealed class CoffeeMachine
{
    public int Id { get; set; }
    public MachineStatus Status { get; set; }
    public int? OrderId { get; set; }
}
