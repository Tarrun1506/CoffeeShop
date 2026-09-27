using CoffeeShopApp.Controllers;
using CoffeeShopApp.Exceptions;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Repository;
using CoffeeShopApp.Services;
using CoffeeShopApp.Storage;
using CoffeeShopApp.Views;

namespace CoffeeShopApp;

internal static class Program
{
    private static async Task Main()
    {
        Console.Title = "Coffee Shop";
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        Logger logger = new();
        logger.Log("App", "Application started.");
        try
        {
            IStorage storage = new JsonStorage();
            IUserRepository userRepository = new UserRepository(storage);
            ICoffeeRepository coffeeRepository = new CoffeeRepository(storage);
            IOrderRepository orderRepository = new OrderRepository(storage);
            IInventoryRepository inventoryRepository = new InventoryRepository(storage);
            IMachineRepository machineRepository = new MachineRepository(storage);

            ICoffeeShopService coffeeShopService = new CoffeeShopService(userRepository, coffeeRepository, orderRepository, inventoryRepository, machineRepository, logger);
            INotificationService notificationService = new NotificationService(
                (_, message) => ConsoleView.Success($"\n☕ Notification: {message}"),
                (order, message) => ConsoleView.Success($"\n✉ Email simulation for {order.CustomerName}: {message}"),
                (order, message) => ConsoleView.Success($"\nSMS simulation for {order.CustomerName}: {message}"));
            notificationService.Subscribe(coffeeShopService);

            await using (coffeeShopService)
            {
                ICoffeeShopController controller = new CoffeeShopController(coffeeShopService, new ConsoleMenuView(), new UserView(), new CoffeeView(), new AdminView(), logger);
                await controller.StartAsync();
            }
        }
        catch (StorageException ex)
        {
            logger.LogError("Storage", ex.ToString());
            ConsoleView.Error($"Storage error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError("Fatal", ex.ToString());
            ConsoleView.Error($"Unexpected application error: {ex.Message}");
        }
        finally { logger.Log("App", "Application stopped."); }
    }
}
