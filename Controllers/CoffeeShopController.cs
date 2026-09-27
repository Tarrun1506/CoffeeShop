using CoffeeShopApp.Constants;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Exceptions;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;
using CoffeeShopApp.Services;
using CoffeeShopApp.Views;

namespace CoffeeShopApp.Controllers;

public sealed class CoffeeShopController : ICoffeeShopController
{
    private readonly ICoffeeShopService _service;
    private readonly ConsoleMenuView _consoleMenu;
    private readonly UserView _userView;
    private readonly CoffeeView _coffeeView;
    private readonly AdminView _adminView;
    private readonly Logger _logger;
    private bool _running = true;

    public CoffeeShopController(ICoffeeShopService service, ConsoleMenuView consoleMenu, UserView userView, CoffeeView coffeeView, AdminView adminView, Logger logger)
    {
        _service = service; _consoleMenu = consoleMenu; _userView = userView; _coffeeView = coffeeView; _adminView = adminView; _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _service.InitializeAsync(ct);
        _logger.Log("Controller", "Coffee shop console started.");
        try
        {
            while (_running)
            {
                _consoleMenu.ShowMainMenu();
                MainMenuOption option = ConsoleInput.ReadEnum<MainMenuOption>("Enter choice: ");
                try
                {
                    switch (option)
                    {
                        case MainMenuOption.Register: await RegisterAsync(ct); break;
                        case MainMenuOption.Login: await LoginAsync(ct); break;
                        case MainMenuOption.Admin: await AdminMenuAsync(ct); break;
                        case MainMenuOption.Exit: _running = false; break;
                    }
                }
                catch (CoffeeShopOperationException ex) { _consoleMenu.DisplayMessage(ex.Message, true); }
                catch (StorageException ex) { _logger.LogError("Storage", ex.ToString()); _consoleMenu.DisplayMessage(ex.Message, true); }
                catch (IOException ex) { _logger.LogError("IO", ex.ToString()); _consoleMenu.DisplayMessage(ex.Message, true); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError("Controller", ex.ToString());
                    _consoleMenu.DisplayMessage("The requested action could not be completed. See CoffeeShop.log for details.", true);
                }
            }
        }
        finally { _logger.Log("Controller", "Coffee shop console stopped."); }
    }

    private async Task RegisterAsync(CancellationToken ct)
    {
        var input = _userView.ReadRegistration();
        User user = await _service.RegisterAsync(input.Name, input.Email, input.Password, input.Method, ct);
        _logger.Log("Register", $"Created customer {user.Id}.");
        _consoleMenu.DisplayMessage($"Account created successfully. Welcome, {user.Name}!");
    }

    private async Task LoginAsync(CancellationToken ct)
    {
        var input = _userView.ReadLogin();
        User? user = await _service.LoginAsync(input.Email, input.Password, ct);
        if (user is null) { _consoleMenu.DisplayMessage(ErrorMessages.InvalidCredentials, true); return; }
        _logger.Log("Login", $"Customer {user.Id} logged in.");
        await CustomerMenuAsync(user, ct);
    }

    private async Task CustomerMenuAsync(User user, CancellationToken ct)
    {
        bool active = true;
        while (active)
        {
            _userView.ShowCustomerMenu(user);
            CustomerMenuOption option = ConsoleInput.ReadEnum<CustomerMenuOption>("Enter choice: ");
            try
            {
                switch (option)
                {
                    case CustomerMenuOption.ViewMenu:
                        _coffeeView.ShowMenu(await _service.GetMenuAsync(ct)); WaitForEnter(); break;
                    case CustomerMenuOption.PlaceOrder: await PlaceOrderAsync(user, ct); break;
                    case CustomerMenuOption.ViewOrders:
                        _coffeeView.ShowOrders(await _service.GetCustomerOrdersAsync(user.Id, ct)); WaitForEnter(); break;
                    case CustomerMenuOption.CancelOrder: await CancelOrderAsync(user, ct); break;
                    case CustomerMenuOption.CompleteOrder: await CompleteOrderAsync(user, ct); break;
                    case CustomerMenuOption.ViewSpending:
                        decimal total = await _service.GetCustomerSpendingAsync(user.Id, ct);
                        _consoleMenu.DisplayMessage($"Total spending (excluding cancelled orders): ₹{total:0.00}"); break;
                    case CustomerMenuOption.Logout: active = false; break;
                }
            }
            catch (CoffeeShopOperationException ex) { _consoleMenu.DisplayMessage(ex.Message, true); }
            catch (StorageException ex) { _logger.LogError("Storage", ex.ToString()); _consoleMenu.DisplayMessage(ex.Message, true); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError("CustomerAction", ex.ToString());
                _consoleMenu.DisplayMessage("The requested action could not be completed. See CoffeeShop.log for details.", true);
            }
        }
    }

    private async Task PlaceOrderAsync(User user, CancellationToken ct)
    {
        var selection = _coffeeView.ReadOrder(await _service.GetMenuAsync(ct));
        Order order = await _service.PlaceOrderAsync(user, selection.Type, selection.Quantity, ct);
        _consoleMenu.DisplayMessage($"Order placed successfully!\nOrder ID: ORD{order.Id}\n{order.Quantity} × {order.CoffeeName}\nTotal: ₹{order.Total:0.00}\nStatus: Placed");
    }

    private async Task CancelOrderAsync(User user, CancellationToken ct)
    {
        _coffeeView.ShowOrders(await _service.GetCustomerOrdersAsync(user.Id, ct));
        int id = _userView.ReadOrderId("Order ID to cancel: ");
        bool cancelled = await _service.CancelOrderAsync(user.Id, id, ct);
        _consoleMenu.DisplayMessage(cancelled ? CommonMessages.OrderCancelled : ErrorMessages.OrderNotCancellable, !cancelled);
    }

    private async Task CompleteOrderAsync(User user, CancellationToken ct)
    {
        _coffeeView.ShowOrders((await _service.GetCustomerOrdersAsync(user.Id, ct)).Where(o => o.Status == OrderStatus.Ready));
        int id = _userView.ReadOrderId("Ready order ID to collect: ");
        bool completed = await _service.CompleteOrderAsync(user.Id, id, ct);
        _consoleMenu.DisplayMessage(completed ? CommonMessages.OrderCompleted : ErrorMessages.OrderNotCompletable, !completed);
    }

    private async Task AdminMenuAsync(CancellationToken ct)
    {
        bool active = true;
        while (active)
        {
            _adminView.ShowMenu();
            AdminMenuOption option = ConsoleInput.ReadEnum<AdminMenuOption>("Enter choice: ");
            switch (option)
            {
                case AdminMenuOption.AllOrders: await ShowOrdersAsync(null, false, ct); break;
                case AdminMenuOption.PreparingOrders: await ShowOrdersAsync(OrderStatus.Preparing, false, ct); break;
                case AdminMenuOption.Machines:
                    _adminView.ShowMachines(await _service.GetMachinesAsync(ct)); WaitForEnter(); break;
                case AdminMenuOption.CompletedOrders: await ShowOrdersAsync(OrderStatus.Completed, false, ct); break;
                case AdminMenuOption.LiveOrders:
                    await _adminView.WatchPreparingOrdersAsync(token => _service.GetOrdersAsync(OrderStatus.Preparing, ct: token), _coffeeView, ct); break;
                case AdminMenuOption.Inventory:
                    _adminView.ShowInventory(await _service.GetInventoryAsync(ct)); WaitForEnter(); break;
                case AdminMenuOption.Restock:
                    await _service.RestockAsync(ct); _consoleMenu.DisplayMessage(CommonMessages.InventoryRestocked); break;
                case AdminMenuOption.TodaysOrders: await ShowOrdersAsync(null, true, ct); break;
                case AdminMenuOption.CoffeeMenu:
                    _coffeeView.ShowMenu(await _service.GetMenuAsync(ct)); WaitForEnter(); break;
                case AdminMenuOption.Back: active = false; break;
            }
        }
    }

    private async Task ShowOrdersAsync(OrderStatus? status, bool todayOnly, CancellationToken ct)
    {
        _coffeeView.ShowOrders(await _service.GetOrdersAsync(status, todayOnly, ct)); WaitForEnter();
    }

    private void WaitForEnter() => _consoleMenu.WaitForEnter();
}
