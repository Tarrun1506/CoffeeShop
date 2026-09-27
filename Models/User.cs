using CoffeeShopApp.Enums;

namespace CoffeeShopApp.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public NotificationMethod NotificationMethod { get; set; } = NotificationMethod.Console;
}
