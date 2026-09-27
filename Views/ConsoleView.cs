using CoffeeShopApp.Helpers;

namespace CoffeeShopApp.Views;

public static class ConsoleView
{
    public static void Write(string message) { lock (ConsoleCoordinator.Gate) Console.Write(message); }
    public static void WriteLine(string message = "") { lock (ConsoleCoordinator.Gate) Console.WriteLine(message); }
    public static void Error(string message)
    {
        lock (ConsoleCoordinator.Gate) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(message); Console.ResetColor(); }
    }
    public static void Success(string message)
    {
        lock (ConsoleCoordinator.Gate) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(message); Console.ResetColor(); }
    }
}
