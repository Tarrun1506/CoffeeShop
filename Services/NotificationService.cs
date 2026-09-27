using CoffeeShopApp.Enums;
using CoffeeShopApp.Events;
using CoffeeShopApp.Models;
using CoffeeShopApp.Views;

namespace CoffeeShopApp.Services;

public sealed class NotificationService : INotificationService
{
    public delegate void NotificationSender(Order order, string message);
    private readonly NotificationSender _console;
    private readonly NotificationSender _email;
    private readonly NotificationSender _sms;

    public NotificationService(NotificationSender console, NotificationSender email, NotificationSender sms)
    {
        _console = console; _email = email; _sms = sms;
    }

    public void Subscribe(ICoffeeShopService service) => service.OrderReady += OnOrderReady;

    private void OnOrderReady(object? sender, OrderChangedEventArgs args)
    {
        Order order = args.Order;
        string message = $"Your {order.CoffeeName} order ORD{order.Id} is ready. Please collect it from Counter 2.";
        NotificationSender senderMethod = order.NotificationMethod switch
        {
            NotificationMethod.EmailSimulation => _email,
            NotificationMethod.SmsSimulation => _sms,
            _ => _console,
        };
        senderMethod(order, message);
    }
}
