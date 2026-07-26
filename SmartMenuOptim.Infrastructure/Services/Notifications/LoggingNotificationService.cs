using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;

namespace SmartMenuOptim.Infrastructure.Services.Notifications;

/// <summary>
/// Logging-based implementation of <see cref="INotificationService"/> for development and testing.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>This implementation logs all notifications instead of sending them through real channels.
/// It serves as a development/testing stub and can be replaced with production implementations
/// for email, SMS, push notifications, or SignalR.</para>
/// 
/// <para><strong>Production Implementations:</strong></para>
/// <para>In production, replace with implementations like:</para>
/// <list type="bullet">
///     <item><description><c>SendGridEmailNotificationService</c> - For email notifications</description></item>
///     <item><description><c>TwilioSmsNotificationService</c> - For SMS notifications</description></item>
///     <item><description><c>SignalRNotificationService</c> - For real-time in-app notifications</description></item>
///     <item><description><c>FirebasePushNotificationService</c> - For mobile push notifications</description></item>
/// </list>
/// </remarks>
public class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendToCustomerAsync(
        int customerId,
        string title,
        string message,
        NotificationType notificationType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NOTIFICATION] To Customer {CustomerId} [{Type}]: {Title} - {Message}",
            customerId,
            notificationType,
            title,
            message);

        return Task.CompletedTask;
    }

    public Task SendToRestaurantStaffAsync(
        int restaurantId,
        string title,
        string message,
        NotificationType notificationType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NOTIFICATION] To Restaurant {RestaurantId} Staff [{Type}]: {Title} - {Message}",
            restaurantId,
            notificationType,
            title,
            message);

        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationAsync(
        int customerId,
        int orderId,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[ORDER CONFIRMATION] Customer {CustomerId}: Order #{OrderId} confirmed for {Amount:C}",
            customerId,
            orderId,
            totalAmount);

        return Task.CompletedTask;
    }

    public Task SendOrderCancellationAsync(
        int customerId,
        int orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[ORDER CANCELLATION] Customer {CustomerId}: Order #{OrderId} cancelled. Reason: {Reason}",
            customerId,
            orderId,
            reason);

        return Task.CompletedTask;
    }

    public Task SendLoyaltyPointsEarnedAsync(
        int customerId,
        int pointsEarned,
        int newBalance,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[LOYALTY POINTS] Customer {CustomerId}: Earned {Points} points! New balance: {Balance}",
            customerId,
            pointsEarned,
            newBalance);

        return Task.CompletedTask;
    }

    public Task SendLoyaltyTierChangedAsync(
        int customerId,
        string previousTier,
        string newTier,
        bool isUpgrade,
        CancellationToken cancellationToken = default)
    {
        var action = isUpgrade ? "Upgraded" : "Changed";
        _logger.LogInformation(
            "[LOYALTY TIER] Customer {CustomerId}: {Action} from {Previous} to {New}",
            customerId,
            action,
            previousTier,
            newTier);

        return Task.CompletedTask;
    }

    public Task SendKitchenOrderAsync(
        int restaurantId,
        int orderId,
        int itemCount,
        string? specialInstructions,
        CancellationToken cancellationToken = default)
    {
        var instructions = string.IsNullOrWhiteSpace(specialInstructions)
            ? "None"
            : specialInstructions;

        _logger.LogInformation(
            "[KITCHEN ORDER] Restaurant {RestaurantId}: New Order #{OrderId} with {Items} items. Instructions: {Instructions}",
            restaurantId,
            orderId,
            itemCount,
            instructions);

        return Task.CompletedTask;
    }
}
