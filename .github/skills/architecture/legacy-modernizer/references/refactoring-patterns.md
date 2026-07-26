# Refactoring Patterns Reference

> **Load when:** Extracting services, applying branch by abstraction, or decomposing monoliths.

## Branch by Abstraction

Swap an implementation without branching the codebase — extract an interface, build a new implementation behind it, and switch via DI or feature flags.

### Step-by-Step Process

```
Step 1: Legacy code calls concrete class directly
┌─────────┐      ┌──────────────────┐
│ Handler  │─────▶│ LegacyEmailSender│
└─────────┘      └──────────────────┘

Step 2: Extract interface, wrap legacy
┌─────────┐      ┌──────────────────┐      ┌──────────────────┐
│ Handler  │─────▶│ IEmailSender     │◀─────│ LegacyEmailSender│
└─────────┘      └──────────────────┘      └──────────────────┘

Step 3: Build new implementation
┌─────────┐      ┌──────────────────┐      ┌──────────────────┐
│ Handler  │─────▶│ IEmailSender     │◀─────│ LegacyEmailSender│
└─────────┘      └──────────────────┘      ├──────────────────┤
                                           │ SendGridSender   │
                                           └──────────────────┘

Step 4: Switch to new, remove legacy
┌─────────┐      ┌──────────────────┐      ┌──────────────────┐
│ Handler  │─────▶│ IEmailSender     │◀─────│ SendGridSender   │
└─────────┘      └──────────────────┘      └──────────────────┘
```

### Implementation Example

```csharp
// Step 1: Extract interface from legacy code
public interface INotificationService
{
    Task SendAsync(Notification notification, CancellationToken ct);
}

// Step 2: Wrap legacy behind interface (no behavior change)
public sealed class LegacySmtpNotificationService : INotificationService
{
    private readonly SmtpClient _smtp;

    public async Task SendAsync(Notification notification, CancellationToken ct)
    {
        // Existing legacy SMTP code — no changes
        var message = new MailMessage("noreply@nextruzt.io", notification.Recipient)
        {
            Subject = notification.Subject,
            Body = notification.Body
        };
        await _smtp.SendMailAsync(message, ct);
    }
}

// Step 3: Build new implementation
public sealed class SendGridNotificationService : INotificationService
{
    private readonly ISendGridClient _client;

    public async Task SendAsync(Notification notification, CancellationToken ct)
    {
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress("noreply@nextruzt.io"),
            new EmailAddress(notification.Recipient),
            notification.Subject,
            notification.Body,
            notification.HtmlBody);
        await _client.SendEmailAsync(msg, ct);
    }
}

// Step 4: Toggle via DI registration
services.AddScoped<INotificationService>(sp =>
{
    var features = sp.GetRequiredService<IFeatureManager>();
    return features.IsEnabledAsync("UseSendGrid").GetAwaiter().GetResult()
        ? sp.GetRequiredService<SendGridNotificationService>()
        : sp.GetRequiredService<LegacySmtpNotificationService>();
});
```

## Extract Service Pattern

Move a cohesive set of functionality from a monolith into a separate service or module.

### Identification Criteria

A module is ready for extraction when:
- It has a clear bounded context with well-defined inputs and outputs
- It changes independently from the rest of the system
- It has minimal shared mutable state with other modules
- It would benefit from independent scaling or deployment

### Extraction Checklist

```markdown
1. [ ] Identify all inbound calls to the module
2. [ ] Identify all outbound calls from the module
3. [ ] Identify shared database tables
4. [ ] Create an interface at the boundary
5. [ ] Replace direct calls with interface calls
6. [ ] Move the implementation behind the interface to a new project/service
7. [ ] Replace shared DB access with API calls or events
8. [ ] Add integration tests at the new boundary
9. [ ] Deploy and monitor independently
```

### Example: Extracting Payment Processing

```csharp
// Before: Payment logic embedded in EscrowService
public sealed class EscrowService
{
    public async Task<EscrowResult> CreateEscrowAsync(CreateEscrowRequest request)
    {
        // Escrow logic
        var escrow = new Escrow(request.BuyerId, request.SellerId, request.Amount);

        // Payment logic interleaved — extraction candidate
        var paymentIntent = await _stripe.CreatePaymentIntentAsync(request.Amount);
        escrow.SetPaymentReference(paymentIntent.Id);

        await _db.SaveChangesAsync();
        return new EscrowResult(escrow.Id);
    }
}

// After: Payment extracted behind interface
public interface IPaymentService
{
    Task<PaymentReference> InitiateHoldAsync(Money amount, string buyerId, CancellationToken ct);
    Task<PaymentResult> CaptureAsync(PaymentReference reference, CancellationToken ct);
    Task ReleaseAsync(PaymentReference reference, CancellationToken ct);
}

public sealed class CreateEscrowHandler : IRequestHandler<CreateEscrowCommand, EscrowResult>
{
    private readonly IPaymentService _payments;
    private readonly IEscrowRepository _repository;

    public async Task<EscrowResult> Handle(CreateEscrowCommand cmd, CancellationToken ct)
    {
        var hold = await _payments.InitiateHoldAsync(cmd.Amount, cmd.BuyerId, ct);
        var escrow = Escrow.Create(cmd.BuyerId, cmd.SellerId, cmd.Amount, hold);
        await _repository.AddAsync(escrow, ct);
        return new EscrowResult(escrow.Id);
    }
}
```

## Parallel Change (Expand-Contract)

A safe refactoring pattern for changing interfaces without breaking consumers:

```
Phase 1 — Expand: Add the new interface alongside the old one
Phase 2 — Migrate: Move all consumers to the new interface
Phase 3 — Contract: Remove the old interface
```

### Database Schema Example

```sql
-- Phase 1: EXPAND — Add new column, keep old
ALTER TABLE escrows ADD COLUMN amount_cents BIGINT;
UPDATE escrows SET amount_cents = CAST(amount * 100 AS BIGINT);

-- Phase 2: MIGRATE — Update application to use amount_cents
-- Deploy code that reads/writes amount_cents

-- Phase 3: CONTRACT — Remove old column after verification
ALTER TABLE escrows DROP COLUMN amount;
ALTER TABLE escrows RENAME COLUMN amount_cents TO amount;
```

## Decompose Conditional

Replace complex conditional logic with polymorphism during modernization:

```csharp
// Before: Switch statement that grows with each new escrow type
public decimal CalculateFee(Escrow escrow) => escrow.Type switch
{
    "standard" => escrow.Amount * 0.025m,
    "premium" => escrow.Amount * 0.015m,
    "enterprise" => escrow.Amount * 0.010m,
    _ => throw new InvalidOperationException($"Unknown type: {escrow.Type}")
};

// After: Strategy pattern — each type owns its fee logic
public interface IFeeStrategy
{
    decimal Calculate(Money amount);
}

public sealed class StandardFeeStrategy : IFeeStrategy
{
    public decimal Calculate(Money amount) => amount.Value * 0.025m;
}

// Register all strategies in DI
services.AddKeyedScoped<IFeeStrategy, StandardFeeStrategy>("standard");
services.AddKeyedScoped<IFeeStrategy, PremiumFeeStrategy>("premium");
services.AddKeyedScoped<IFeeStrategy, EnterpriseFeeStrategy>("enterprise");
```
