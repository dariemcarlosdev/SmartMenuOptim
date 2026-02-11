namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Interface for sending notifications to customers and staff.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Abstracts notification delivery mechanisms, allowing for multiple implementations
/// (email, SMS, push notifications, in-app notifications) without changing application logic.</para>
/// 
/// <para><strong>Clean Architecture:</strong></para>
/// <para>Interface defined in Application layer, implementations in Infrastructure layer.</para>
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a specific customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message body.</param>
    /// <param name="notificationType">Type of notification for routing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendToCustomerAsync(
        int customerId,
        string title,
        string message,
        NotificationType notificationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to restaurant staff.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message body.</param>
    /// <param name="notificationType">Type of notification for routing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendToRestaurantStaffAsync(
        int restaurantId,
        string title,
        string message,
        NotificationType notificationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an order confirmation notification.
    /// </summary>
    Task SendOrderConfirmationAsync(
        int customerId,
        int orderId,
        decimal totalAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an order cancellation notification.
    /// </summary>
    Task SendOrderCancellationAsync(
        int customerId,
        int orderId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a loyalty points earned notification.
    /// </summary>
    Task SendLoyaltyPointsEarnedAsync(
        int customerId,
        int pointsEarned,
        int newBalance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a loyalty tier change notification.
    /// </summary>
    Task SendLoyaltyTierChangedAsync(
        int customerId,
        string previousTier,
        string newTier,
        bool isUpgrade,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a kitchen order notification.
    /// </summary>
    Task SendKitchenOrderAsync(
        int restaurantId,
        int orderId,
        int itemCount,
        string? specialInstructions,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of notifications for routing and prioritization.
/// </summary>
public enum NotificationType
{
    /// <summary>General information notification.</summary>
    Info = 0,

    /// <summary>Order-related notification.</summary>
    Order = 1,

    /// <summary>Loyalty program notification.</summary>
    Loyalty = 2,

    /// <summary>Promotional notification.</summary>
    Promotion = 3,

    /// <summary>Kitchen/Staff alert.</summary>
    KitchenAlert = 4,

    /// <summary>System alert.</summary>
    SystemAlert = 5
}
