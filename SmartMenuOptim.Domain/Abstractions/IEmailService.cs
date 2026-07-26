namespace SmartMenuOptim.Domain.Abstractions;

/// <summary>
/// Domain abstraction for email sending services.
/// This is a PORT in Hexagonal Architecture - defines what the domain needs without specifying how.
/// </summary>
/// <remarks>
/// <para><strong>Hexagonal Architecture (Ports & Adapters)</strong></para>
/// 
/// This interface is a PRIMARY PORT that defines the domain's email requirements.
/// Implementations (ADAPTERS) reside in the Infrastructure layer.
/// 
/// <para><strong>Design Principles:</strong></para>
/// <list type="bullet">
///   <item><description>Domain defines WHAT it needs (this interface)</description></item>
///   <item><description>Infrastructure defines HOW (SMTP, SendGrid, etc.)</description></item>
///   <item><description>No coupling to specific email providers</description></item>
/// </list>
/// </remarks>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body content.</param>
    /// <param name="isHtml">Whether the body is HTML formatted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if email was sent successfully.</returns>
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients.
    /// </summary>
    /// <param name="recipients">Collection of recipient email addresses.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body content.</param>
    /// <param name="isHtml">Whether the body is HTML formatted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all emails were sent successfully.</returns>
    Task<bool> SendBulkEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken cancellationToken = default);
}
