using CoffeeShopApp.Constants;
using CoffeeShopApp.Helpers;
using System.Text;

namespace CoffeeShopApp.Views;

public static class ConsoleInput
{
    public static int ReadIntInRange(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max) return value;
            ConsoleView.Error(ErrorMessages.InvalidMenuChoice);
        }
    }

    public static string ReadRequiredText(string prompt, Func<string, string?> validate)
    {
        while (true)
        {
            Console.Write(prompt);
            string value = Console.ReadLine()?.Trim() ?? string.Empty;
            string? error = validate(value);
            if (error is null) return value;
            ConsoleView.Error(error);
        }
    }

    public static T ReadEnum<T>(string prompt) where T : struct, Enum
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int choice) && Enum.IsDefined(typeof(T), choice)) return (T)Enum.ToObject(typeof(T), choice);
            ConsoleView.Error(ErrorMessages.InvalidMenuChoice);
        }
    }

    public static string ReadPasswordMasked(string prompt, Func<string, string?> validate)
    {
        while (true)
        {
            ConsoleView.Write(prompt);
            string password = ReadMaskedLine();
            string? error = validate(password);
            if (error is null) return password;
            ConsoleView.Error(error);
        }
    }

    private static string ReadMaskedLine()
    {
        // Keep redirected input usable for scripted runs; interactive input is always masked.
        if (Console.IsInputRedirected) return Console.ReadLine() ?? string.Empty;

        StringBuilder value = new();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                ConsoleView.WriteLine();
                return value.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value.Length--;
                    ConsoleView.Write("\b \b");
                }
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                value.Append(key.KeyChar);
                ConsoleView.Write("*");
            }
        }
    }
}
