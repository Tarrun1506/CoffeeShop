using CoffeeShopApp.Constants;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Models;
using CoffeeShopApp.Storage;

namespace CoffeeShopApp.Repository;

public sealed class MachineRepository(IStorage storage) : IMachineRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<IReadOnlyList<CoffeeMachine>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<CoffeeMachine> machines = await storage.LoadAsync<CoffeeMachine>(Path.GetFileName(Configurables.MachinesFile), ct);
            if (machines.Count == 0)
            {
                machines = Enumerable.Range(1, Configurables.MachineCount).Select(id => new CoffeeMachine { Id = id, Status = MachineStatus.Available }).ToList();
                await SaveMachinesAsync(machines, ct);
            }
            return machines.OrderBy(m => m.Id).ToList();
        }
        finally { _gate.Release(); }
    }
    public async Task UpdateAsync(CoffeeMachine machine, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            List<CoffeeMachine> machines = await storage.LoadAsync<CoffeeMachine>(Path.GetFileName(Configurables.MachinesFile), ct);
            int index = machines.FindIndex(m => m.Id == machine.Id);
            if (index < 0) throw new KeyNotFoundException($"Machine {machine.Id} was not found.");
            machines[index] = machine; await SaveMachinesAsync(machines, ct);
        }
        finally { _gate.Release(); }
    }
    private Task SaveMachinesAsync(List<CoffeeMachine> machines, CancellationToken ct) => storage.SaveAsync(Path.GetFileName(Configurables.MachinesFile), machines, ct);
}
