namespace CoffeeShopApp.Exceptions;

public sealed class StorageException(string message, Exception innerException) : Exception(message, innerException);
