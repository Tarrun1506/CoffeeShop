using System.Net.Mail;
using CoffeeShopApp.Enums;
using CoffeeShopApp.Helpers;
using CoffeeShopApp.Models;

namespace CoffeeShopApp.Views;

public sealed class UserView
{
    public (string Name, string Email, string Password, NotificationMethod Method) ReadRegistration()
    {
        ConsoleView.WriteLine("===== REGISTER =====");
        string name = ConsoleInput.ReadRequiredText("Name: ", value => value.Length is >= 2 and <= 60 ? null : "Name must contain 2 to 60 characters.");
        string email = ConsoleInput.ReadRequiredText("Email: ", value => MailAddress.TryCreate(value, out _) ? null : "Enter a valid email address.");
        string password = ConsoleInput.ReadPasswordMasked("Password (8+ characters): ", value => value.Length is >= 8 and <= 256 ? null : "Password must contain between 8 and 256 characters.");
        ConsoleView.WriteLine("Notification: 1. Console  2. Email simulation  3. SMS simulation");
        return (name, email, password, ConsoleInput.ReadEnum<NotificationMethod>("Choice: "));
    }

    public (string Email, string Password) ReadLogin()
    {
        ConsoleView.WriteLine("===== LOGIN =====");
        return (ConsoleInput.ReadRequiredText("Email: ", value => MailAddress.TryCreate(value, out _) ? null : "Enter a valid email address."), ConsoleInput.ReadPasswordMasked("Password: ", value => value.Length > 0 ? null : "Password is required."));
    }

    public void ShowCustomerMenu(User user)
    {
        ConsoleView.WriteLine($"===== Welcome, {user.Name} =====");
        ConsoleView.WriteLine("1. View coffee menu\n2. Place an order\n3. View my orders\n4. Cancel a placed order\n5. Mark ready order collected\n6. View total spending\n0. Logout");
    }

    public int ReadOrderId(string prompt) => ConsoleInput.ReadIntInRange(prompt, 1001, int.MaxValue);
}
