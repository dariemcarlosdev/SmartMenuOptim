# Mitigation Catalog

Countermeasures organized by STRIDE category for .NET/Blazor/Azure applications.

## Spoofing Mitigations

| Mitigation | Implementation | Effort |
|-----------|---------------|--------|
| **Entra ID Authentication** | `AddMicrosoftIdentityWebApp()` in `Program.cs` | M |
| **JWT Validation** | `AddJwtBearer()` with issuer, audience, signing key | S |
| **Managed Identity** | `DefaultAzureCredential()` for service-to-service | S |
| **Mutual TLS** | Configure client certificates in Kestrel | L |
| **Message Signing** | HMAC-SHA256 on message payloads in Service Bus | M |

```csharp
// Entra ID + JWT Validation
builder.Services.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Managed Identity for Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri(vaultUri),
    new DefaultAzureCredential());
```

## Tampering Mitigations

| Mitigation | Implementation | Effort |
|-----------|---------------|--------|
| **TLS Everywhere** | `app.UseHttpsRedirection()` + HSTS | S |
| **Input Validation** | FluentValidation on all commands | M |
| **Anti-Forgery** | `[ValidateAntiForgeryToken]` on forms | S |
| **Parameterized Queries** | EF Core LINQ (default) or `ExecuteSqlInterpolated` | S |
| **Audit Trail** | Append-only audit table with triggers | M |
| **Digital Signatures** | Sign escrow state transitions with asymmetric keys | L |

```csharp
// FluentValidation for escrow command
public sealed class CreateEscrowValidator : AbstractValidator<CreateEscrowCommand>
{
    public CreateEscrowValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(Money.Zero)
            .WithMessage("Amount must be positive");
        RuleFor(x => x.BuyerId).NotEmpty();
        RuleFor(x => x.SellerId).NotEmpty()
            .NotEqual(x => x.BuyerId)
            .WithMessage("Buyer and seller must be different");
        RuleFor(x => x.Deadline).GreaterThan(DateTimeOffset.UtcNow.AddHours(1));
    }
}
```

## Repudiation Mitigations

| Mitigation | Implementation | Effort |
|-----------|---------------|--------|
| **Structured Audit Logging** | Serilog + Azure Monitor / Seq | M |
| **Tamper-Proof Storage** | Immutable Azure Blob or append-only SQL table | M |
| **Correlation IDs** | `Activity.Current.Id` propagation | S |
| **Transaction Signatures** | Digital signatures on escrow operations | L |

```csharp
// Audit logging with Serilog
Log.Information("Escrow {Action} by {UserId}: {EscrowId} amount {Amount}",
    "Released", currentUser.Id, escrow.Id, escrow.Amount);
// Stored in tamper-proof Azure Monitor
```

## Information Disclosure Mitigations

| Mitigation | Implementation | Effort |
|-----------|---------------|--------|
| **Generic Error Pages** | `app.UseExceptionHandler("/Error")` in production | S |
| **Remove Server Headers** | Strip `Server`, `X-Powered-By` headers | S |
| **Log Redaction** | Serilog `Destructure.ByTransforming<>()` for PII | M |
| **Data Protection API** | Encrypt sensitive fields at rest | M |
| **Security Headers** | CSP, X-Content-Type-Options, X-Frame-Options | S |

```csharp
// Security headers middleware
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    ctx.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");
    ctx.Response.Headers.Remove("Server");
    ctx.Response.Headers.Remove("X-Powered-By");
    await next();
});
```

## Denial of Service Mitigations

| Mitigation | Implementation | Effort |
|-----------|---------------|--------|
| **Rate Limiting** | `AddRateLimiter()` in ASP.NET Core 7+ | S |
| **Request Size Limits** | `MaxRequestBodySize` in Kestrel | S |
| **Query Pagination** | `Skip().Take()` on all list queries | S |
| **Circuit Breakers** | Polly `CircuitBreakerAsync` on external calls | M |
| **Timeout Policies** | Polly `TimeoutAsync` on all HTTP calls | S |
| **Bulkhead Isolation** | Polly `BulkheadAsync` for critical paths | M |
| **SignalR Limits** | `MaximumReceiveMessageSize`, connection limits | S |

```csharp
// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("escrow-create", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

// Polly resilience
builder.Services.AddHttpClient<IPaymentGateway, StripeGateway>()
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)))
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

## Elevation of Privilege Mitigations

| Mitigation | Implementation | Effort |
|-----------|---------------|--------|
| **Policy-Based Auth** | `[Authorize(Policy = "EscrowAdmin")]` | S |
| **Resource-Based Auth** | `IAuthorizationService.AuthorizeAsync()` | M |
| **IDOR Prevention** | GUIDs + ownership filter in queries | S |
| **Safe Deserialization** | `System.Text.Json` typed deserialization | S |
| **Path Validation** | `Path.GetFullPath()` + prefix check | S |
| **Least Privilege** | Managed Identity with minimal RBAC | M |

## Mitigation Priority Matrix

Select mitigations based on DREAD score and effort:

| DREAD Risk \ Effort | Small (S) | Medium (M) | Large (L) |
|---------------------|-----------|-----------|-----------|
| **Critical (12-15)** | Do NOW | Do NOW | Plan + Do ASAP |
| **High (8-11)** | Do this sprint | Do this sprint | Plan for next sprint |
| **Medium (5-7)** | Do when convenient | Plan for next release | Backlog |
| **Low (1-4)** | Quick wins only | Backlog | Accept risk |

**Quick wins** (Critical/High + Small effort): Rate limiting, security headers, HTTPS redirect, generic error pages, `[Authorize]` attributes.
