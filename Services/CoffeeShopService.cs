using System.Threading.Channels;
using CoffeeShopApp.Constants;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Events;
using CoffeeShopApp.Exceptions;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;
using CoffeeShopApp.Repository;

namespace CoffeeShopApp.Services;

public sealed class CoffeeShopService : ICoffeeShopService
{
    private readonly IUserRepository _users;
    private readonly ICoffeeRepository _coffees;
    private readonly IOrderRepository _orders;
    private readonly IInventoryRepository _inventory;
    private readonly IMachineRepository _machines;
    private readonly Logger _logger;
    private readonly Channel<PreparationJob> _queue = Channel.CreateUnbounded<PreparationJob>(new() { SingleReader = false, SingleWriter = false });
    private readonly SemaphoreSlim _transitions = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly List<Task> _workers = [];
    private bool _started;

    public event EventHandler<OrderChangedEventArgs>? OrderReady;

    public CoffeeShopService(IUserRepository users, ICoffeeRepository coffees, IOrderRepository orders, IInventoryRepository inventory, IMachineRepository machines, Logger logger)
    {
        _users = users; _coffees = coffees; _orders = orders; _inventory = inventory; _machines = machines; _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_started) return;
        _ = await _coffees.GetAllAsync(ct);
        _ = await _inventory.GetAllAsync(ct);
        IReadOnlyList<CoffeeMachine> machines = await _machines.GetAllAsync(ct);
        if (machines.Count != Configurables.MachineCount) throw new CoffeeShopOperationException("Coffee shop data must contain exactly two machines.");

        foreach (CoffeeMachine machine in machines)
            if (machine.Status != MachineStatus.Available || machine.OrderId.HasValue)
                await _machines.UpdateAsync(new CoffeeMachine { Id = machine.Id, Status = MachineStatus.Available }, ct);

        IReadOnlyList<Order> savedOrders = await _orders.GetAllAsync(ct);
        foreach (Order order in savedOrders.Where(o => o.Status == OrderStatus.Preparing))
        {
            order.Status = OrderStatus.Placed;
            await _orders.UpdateAsync(order, ct);
        }

        _started = true;
        foreach (CoffeeMachine machine in machines.OrderBy(m => m.Id))
            _workers.Add(Task.Run(() => RunMachineAsync(machine.Id, _shutdown.Token)));
        _workers.Add(Task.Run(() => RestockLoopAsync(_shutdown.Token)));
        foreach (Order order in savedOrders.Where(o => o.Status is OrderStatus.Placed or OrderStatus.Preparing))
            for (int unit = order.UnitsPrepared; unit < order.Quantity; unit++)
                await _queue.Writer.WriteAsync(new PreparationJob(order.Id), ct);
        _logger.Log("Startup", $"Started {machines.Count} machine workers; restored placed work.");
    }

    public async Task<User> RegisterAsync(string name, string email, string password, NotificationMethod method, CancellationToken ct = default)
    {
        name = name.Trim(); email = email.Trim();
        if (name.Length is < 2 or > 60) throw new CoffeeShopOperationException(ErrorMessages.InvalidName);
        if (email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email, out _)) throw new CoffeeShopOperationException(ErrorMessages.InvalidEmail);
        if (password.Length is < 8 or > 256) throw new CoffeeShopOperationException("Password must contain between 8 and 256 characters.");
        if (!Enum.IsDefined(method)) throw new CoffeeShopOperationException(ErrorMessages.InvalidMenuChoice);
        var credentials = PasswordHasher.Create(password);
        User user = new() { Name = name, Email = email, PasswordHash = credentials.Hash, PasswordSalt = credentials.Salt, NotificationMethod = method };
        try { await _users.AddAsync(user, ct); }
        catch (InvalidOperationException ex) { throw new CoffeeShopOperationException(ex.Message); }
        _logger.Log("Register", $"Registered user {user.Id}.");
        return user;
    }

    public async Task<User?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        User? user = await _users.FindByEmailAsync(email.Trim(), ct);
        return user is not null && PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash) ? user : null;
    }

    public Task<IReadOnlyList<Coffee>> GetMenuAsync(CancellationToken ct = default) => _coffees.GetAllAsync(ct);

    public async Task<Order> PlaceOrderAsync(User customer, CoffeeType type, int quantity, CancellationToken ct = default)
    {
        if (quantity is < 1 or > Configurables.MaximumOrderQuantity) throw new CoffeeShopOperationException(ErrorMessages.InvalidQuantity);
        Coffee coffee = await _coffees.GetAsync(type, ct) ?? throw new CoffeeShopOperationException("That coffee is not on the menu.");
        if (!coffee.IsAvailable) throw new CoffeeShopOperationException($"{coffee.Name} is currently unavailable.");
        if (!await _inventory.ReserveAsync(coffee, quantity, ct)) throw new CoffeeShopOperationException(ErrorMessages.InsufficientStock);

        Order order = new() { CustomerId = customer.Id, CustomerName = customer.Name, CoffeeType = coffee.Type, CoffeeName = coffee.Name, Quantity = quantity, UnitPrice = coffee.Price, NotificationMethod = customer.NotificationMethod };
        bool saved = false;
        try
        {
            await _orders.AddAsync(order, ct);
            saved = true;
            // Once stock is reserved, finish enqueueing independently of a caller disconnect/cancel.
            for (int unit = 0; unit < quantity; unit++)
                await _queue.Writer.WriteAsync(new PreparationJob(order.Id), CancellationToken.None);
            return order;
        }
        catch
        {
            if (saved)
            {
                order.Status = OrderStatus.Cancelled;
                try { await _orders.UpdateAsync(order, CancellationToken.None); } catch (Exception ex) { _logger.LogError("OrderRollback", ex.Message); }
            }
            await _inventory.ReleaseAsync(coffee, quantity, CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> CancelOrderAsync(Guid customerId, int orderId, CancellationToken ct = default)
    {
        await _transitions.WaitAsync(ct);
        try
        {
            Order? order = await _orders.GetAsync(orderId, ct);
            if (order is null || order.CustomerId != customerId || order.Status != OrderStatus.Placed) return false;
            order.Status = OrderStatus.Cancelled;
            await _orders.UpdateAsync(order, ct);
            Coffee? coffee = await _coffees.GetAsync(order.CoffeeType, ct);
            if (coffee is not null) await _inventory.ReleaseAsync(coffee, order.Quantity, ct);
            return true;
        }
        finally { _transitions.Release(); }
    }

    public async Task<bool> CompleteOrderAsync(Guid customerId, int orderId, CancellationToken ct = default)
    {
        await _transitions.WaitAsync(ct);
        try
        {
            Order? order = await _orders.GetAsync(orderId, ct);
            if (order is null || order.CustomerId != customerId || order.Status != OrderStatus.Ready) return false;
            order.Status = OrderStatus.Completed;
            await _orders.UpdateAsync(order, ct);
            return true;
        }
        finally { _transitions.Release(); }
    }

    public async Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, CancellationToken ct = default) =>
        (await _orders.GetByCustomerAsync(customerId, ct)).OrderByDescending(o => o.CreatedAt).ToList();

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(OrderStatus? status = null, bool todayOnly = false, CancellationToken ct = default)
    {
        IEnumerable<Order> orders = await _orders.GetAllAsync(ct);
        if (status.HasValue) orders = orders.Where(o => o.Status == status.Value);
        if (todayOnly) orders = orders.Where(o => o.CreatedAt.Date == DateTime.Today);
        return orders.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public async Task<decimal> GetCustomerSpendingAsync(Guid customerId, CancellationToken ct = default) =>
        (await _orders.GetByCustomerAsync(customerId, ct)).Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);

    public Task<IReadOnlyList<CoffeeMachine>> GetMachinesAsync(CancellationToken ct = default) => _machines.GetAllAsync(ct);
    public Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(CancellationToken ct = default) => _inventory.GetAllAsync(ct);
    public async Task RestockAsync(CancellationToken ct = default) { await _inventory.RestockAsync(ct); _logger.Log("Inventory", "Inventory restocked by admin."); }

    public async Task<CoffeeShopSnapshot> GetDashboardSnapshotAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Order> orders = await _orders.GetAllAsync(ct);
        IReadOnlyList<CoffeeMachine> machines = await _machines.GetAllAsync(ct);
        return new CoffeeShopSnapshot
        {
            TotalOrders = orders.Count,
            PlacedOrders = orders.Count(o => o.Status == OrderStatus.Placed),
            PreparingOrders = orders.Count(o => o.Status == OrderStatus.Preparing),
            ReadyOrders = orders.Count(o => o.Status == OrderStatus.Ready),
            CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            Machines = machines,
            ActiveOrders = orders.Where(o => o.Status is OrderStatus.Placed or OrderStatus.Preparing or OrderStatus.Ready).OrderBy(o => o.CreatedAt).Take(3).ToList(),
            LatestReadyOrder = orders.Where(o => o.Status is OrderStatus.Ready or OrderStatus.Completed).OrderByDescending(o => o.ReadyAt).FirstOrDefault(),
            CompletedRevenue = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.Total),
        };
    }

    private async Task RunMachineAsync(int machineId, CancellationToken ct)
    {
        try
        {
            await foreach (PreparationJob job in _queue.Reader.ReadAllAsync(ct))
            {
                Order? order = null;
                bool machineClaimed = false;
                try
                {
                    await _transitions.WaitAsync(ct);
                    try
                    {
                        order = await _orders.GetAsync(job.OrderId, ct);
                        if (order is null || order.Status is not (OrderStatus.Placed or OrderStatus.Preparing) || order.UnitsPrepared >= order.Quantity)
                            continue;

                        await _machines.UpdateAsync(new CoffeeMachine { Id = machineId, Status = MachineStatus.Busy, OrderId = order.Id }, ct);
                        machineClaimed = true;
                        order.Status = OrderStatus.Preparing;
                        await _orders.UpdateAsync(order, ct);
                    }
                    finally { _transitions.Release(); }

                    using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
                    Coffee coffee = await _coffees.GetAsync(order!.CoffeeType, ct) ?? throw new CoffeeShopOperationException("The ordered coffee was removed from the menu.");
                    await WaitForTimerAsync(timer, coffee.SourcingSeconds, ct);
                    await WaitForTimerAsync(timer, coffee.PreparationSeconds, ct);

                    await _transitions.WaitAsync(ct);
                    try
                    {
                        Order? latest = await _orders.GetAsync(order.Id, ct);
                        if (latest is null || latest.Status != OrderStatus.Preparing) continue;
                        latest.UnitsPrepared = Math.Min(latest.Quantity, latest.UnitsPrepared + 1);
                        bool ready = latest.UnitsPrepared >= latest.Quantity;
                        if (ready)
                        {
                            latest.Status = OrderStatus.Ready;
                            latest.ReadyAt = DateTime.Now;
                        }
                        await _orders.UpdateAsync(latest, ct);
                        if (ready) OrderReady?.Invoke(this, new OrderChangedEventArgs(latest));
                    }
                    finally { _transitions.Release(); }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError($"Machine {machineId}", ex.ToString());
                    if (order is not null) await HandlePreparationFailureAsync(order.Id, job);
                }
                finally
                {
                    if (machineClaimed) await ReleaseMachineAsync(machineId);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError($"MachineWorker {machineId}", ex.ToString()); }
    }

    private async Task RestockLoopAsync(CancellationToken ct)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(Configurables.RestockIntervalMinutes));
        try
        {
            while (await timer.WaitForNextTickAsync(ct)) await _inventory.RestockAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { _logger.LogError("RestockWorker", ex.ToString()); }
    }

    private async Task HandlePreparationFailureAsync(int orderId, PreparationJob job)
    {
        await _transitions.WaitAsync(CancellationToken.None);
        try
        {
            Order? latest = await _orders.GetAsync(orderId, CancellationToken.None);
            if (latest is null || latest.Status is not (OrderStatus.Placed or OrderStatus.Preparing)) return;
            latest.PreparationFailures++;
            bool cancel = latest.PreparationFailures >= 3;
            if (cancel) latest.Status = OrderStatus.Cancelled;
            await _orders.UpdateAsync(latest, CancellationToken.None);
            if (!cancel) _queue.Writer.TryWrite(job);
            else
            {
                Coffee? coffee = await _coffees.GetAsync(latest.CoffeeType, CancellationToken.None);
                if (coffee is not null) await _inventory.ReleaseAsync(coffee, latest.Quantity, CancellationToken.None);
            }
        }
        catch (Exception ex) { _logger.LogError("PreparationRecovery", ex.ToString()); }
        finally { _transitions.Release(); }
    }

    private async Task ReleaseMachineAsync(int machineId)
    {
        await _transitions.WaitAsync(CancellationToken.None);
        try
        {
            await _machines.UpdateAsync(new CoffeeMachine { Id = machineId, Status = MachineStatus.Available }, CancellationToken.None);
        }
        catch (Exception ex) { _logger.LogError("MachineRelease", ex.ToString()); }
        finally { _transitions.Release(); }
    }

    private static async Task WaitForTimerAsync(PeriodicTimer timer, int seconds, CancellationToken ct)
    {
        for (int i = 0; i < seconds; i++) await timer.WaitForNextTickAsync(ct);
    }

    private sealed record PreparationJob(int OrderId);

    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        _shutdown.Cancel();
        try { await Task.WhenAll(_workers); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.LogError("Shutdown", ex.ToString()); }
        _shutdown.Dispose();
        _transitions.Dispose();
    }
}
