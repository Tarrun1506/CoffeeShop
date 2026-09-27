namespace CoffeeShopApp.Views;

public sealed class ConsoleMenuView
{
    public void ShowMainMenu() => ConsoleView.WriteLine("===== COFFEE SHOP =====\n1. Register\n2. Login\n3. Admin console\n0. Exit");

    public void DisplayMessage(string message, bool isError = false)
    {
        if (isError) ConsoleView.Error(message);
        else ConsoleView.WriteLine(message);
        WaitForEnter();
    }

    public void WaitForEnter()
    {
        ConsoleView.WriteLine("\nPress Enter to continue...");
        Console.ReadLine();
    }
}
