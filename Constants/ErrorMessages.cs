namespace CoffeeShopApp.Constants;

public static class ErrorMessages
{
    public const string InvalidMenuChoice = "Invalid choice. Please select one of the listed options.";
    public const string InvalidEmail = "Enter a valid email address.";
    public const string InvalidPassword = "Password must contain at least 8 characters.";
    public const string InvalidName = "Name must contain between 2 and 60 characters.";
    public const string DuplicateEmail = "An account with that email already exists.";
    public const string InvalidCredentials = "Email or password was incorrect.";
    public const string InvalidQuantity = "Quantity must be between 1 and 20.";
    public const string InsufficientStock = "There is not enough stock to prepare that order.";
    public const string OrderNotCancellable = "Order not found or preparation has already started.";
    public const string OrderNotCompletable = "Only your ready orders can be completed.";
    public const string FileReadFailed = "Failed to read application data.";
    public const string FileWriteFailed = "Failed to save application data.";
    public const string InvalidData = "Application data is invalid or corrupted.";
}
