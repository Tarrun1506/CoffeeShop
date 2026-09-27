namespace CoffeeShopApp.Constants;

public static class Configurables
{
    public static string DataDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    public static string UsersFile => Path.Combine(DataDirectory, "Users.json");
    public static string OrdersFile => Path.Combine(DataDirectory, "Orders.json");
    public static string MenuFile => Path.Combine(DataDirectory, "Menu.json");
    public static string InventoryFile => Path.Combine(DataDirectory, "Inventory.json");
    public static string MachinesFile => Path.Combine(DataDirectory, "Machines.json");
    public static string LogFile => Path.Combine(DataDirectory, "CoffeeShop.log");
    public const int MachineCount = 2;
    public const int MaximumOrderQuantity = 20;
    public const int RestockIntervalMinutes = 5;
    public static readonly TimeSpan LiveMonitorRefreshInterval = TimeSpan.FromSeconds(1);
}
