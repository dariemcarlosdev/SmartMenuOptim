namespace SmartMenuOptim.Domain.Abstractions;

/// <summary>
/// Domain abstraction for notification services.
/// This is a PORT in Hexagonal Architecture - defines what the domain needs without specifying how.
/// </summary>
/// <remarks>
/// <para><strong>Hexagonal Architecture (Ports & Adapters)</strong></para>
/// 
/// This interface is a PRIMARY PORT that defines the domain's notification requirements.
/// Implementations (ADAPTERS) reside in the Infrastructure layer.
/// 
/// <para><strong>Notification Channels:</strong></para>
/// <list type="bullet">
///   <item><description>Push notifications (mobile/web)</description></item>
///   <item><description>SMS notifications</description></item>
///   <item><description>In-app notifications</description></item>
///   <item><description>Real-time SignalR notifications</description></item>
/// </list>
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    /// <param name="userId">The user identifier to notify.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message content.</param>
    /// <param name="notificationType">Type of notification (info, warning, success, error).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if notification was sent successfully.</returns>
    Task<bool> SendNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType notificationType = NotificationType.Info,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to multiple users.
    /// </summary>
    /// <param name="userIds">Collection of user identifiers to notify.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message content.</param>
    /// <param name="notificationType">Type of notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all notifications were sent successfully.</returns>
    Task<bool> SendBulkNotificationAsync(
        IEnumerable<string> userIds,
        string title,
        string message,
        NotificationType notificationType = NotificationType.Info,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to all users of a specific restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message content.</param>
    /// <param name="notificationType">Type of notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if notifications were sent successfully.</returns>
    Task<bool> SendRestaurantNotificationAsync(
        int restaurantId,
        string title,
        string message,
        NotificationType notificationType = NotificationType.Info,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of notifications that can be sent.
/// </summary>
public enum NotificationType
{
    /// <summary>Informational notification.</summary>
    Info,
    
    /// <summary>Success notification.</summary>
    Success,
    
    /// <summary>Warning notification.</summary>
    Warning,
    
    /// <summary>Error notification.</summary>
    Error,
    
    /// <summary>Alert requiring immediate attention.</summary>
    Alert
}
