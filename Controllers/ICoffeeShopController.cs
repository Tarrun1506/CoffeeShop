namespace CoffeeShopApp.Controllers;

public interface ICoffeeShopController
{
    Task StartAsync(CancellationToken ct = default);
}
