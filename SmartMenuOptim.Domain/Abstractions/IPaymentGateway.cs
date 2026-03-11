namespace SmartMenuOptim.Domain.Abstractions;

/// <summary>
/// Domain abstraction for payment processing services.
/// This is a PORT in Hexagonal Architecture - defines what the domain needs without specifying how.
/// </summary>
/// <remarks>
/// <para><strong>Hexagonal Architecture (Ports & Adapters)</strong></para>
/// 
/// This interface is a PRIMARY PORT that defines the domain's payment requirements.
/// Implementations (ADAPTERS) reside in the Infrastructure layer (Stripe, PayPal, etc.).
/// 
/// <para><strong>Design Principles:</strong></para>
/// <list type="bullet">
///   <item><description>Domain defines payment business rules</description></item>
///   <item><description>Infrastructure handles provider-specific logic</description></item>
///   <item><description>Easy to swap payment providers</description></item>
/// </list>
/// </remarks>
public interface IPaymentGateway
{
    /// <summary>
    /// Processes a payment for an order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="amount">Payment amount.</param>
    /// <param name="currency">Currency code (e.g., "USD", "EUR").</param>
    /// <param name="paymentMethodId">Payment method identifier from the provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment result with transaction details.</returns>
    Task<PaymentResult> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        string currency,
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a previously processed payment.
    /// </summary>
    /// <param name="transactionId">The original transaction identifier.</param>
    /// <param name="amount">Amount to refund (null for full refund).</param>
    /// <param name="reason">Reason for the refund.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refund result with details.</returns>
    Task<RefundResult> RefundPaymentAsync(
        string transactionId,
        decimal? amount = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a payment method.
    /// </summary>
    /// <param name="paymentMethodId">Payment method identifier to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the payment method is valid and can be charged.</returns>
    Task<bool> ValidatePaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a payment processing operation.
/// </summary>
public class PaymentResult
{
    /// <summary>Whether the payment was successful.</summary>
    public bool Success { get; set; }
    
    /// <summary>Transaction identifier from the payment provider.</summary>
    public string? TransactionId { get; set; }
    
    /// <summary>Error message if payment failed.</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>Error code from the payment provider.</summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>Amount that was charged.</summary>
    public decimal AmountCharged { get; set; }
    
    /// <summary>Currency of the charge.</summary>
    public string? Currency { get; set; }
    
    /// <summary>Timestamp of the transaction.</summary>
    public DateTime TransactionDate { get; set; }
}

/// <summary>
/// Result of a refund operation.
/// </summary>
public class RefundResult
{
    /// <summary>Whether the refund was successful.</summary>
    public bool Success { get; set; }
    
    /// <summary>Refund transaction identifier.</summary>
    public string? RefundId { get; set; }
    
    /// <summary>Error message if refund failed.</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>Amount that was refunded.</summary>
    public decimal AmountRefunded { get; set; }
    
    /// <summary>Timestamp of the refund.</summary>
    public DateTime RefundDate { get; set; }
}
