namespace CoffeeShopApp.Exceptions;

public sealed class CoffeeShopOperationException(string message) : Exception(message);
