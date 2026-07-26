# STRIDE Analysis Guide

Detailed questions, threat examples, and mitigations for each STRIDE category.

## S — Spoofing (Authentication Threats)

**Core Question:** Can an attacker pretend to be someone or something else?

### Threats by Component

| Component | Threat | Example |
|-----------|--------|---------|
| API Endpoint | Identity spoofing | Forged JWT tokens, stolen session cookies |
| External Service | Service impersonation | DNS hijacking redirecting API calls |
| Message Queue | Origin spoofing | Unauthorized publisher sends malicious messages |
| Database | Connection spoofing | Attacker connects with stolen credentials |
| SignalR Hub | Circuit hijacking | Reconnecting to another user's Blazor circuit |

### Detection Questions
- [ ] Is every endpoint protected with authentication?
- [ ] Are JWT tokens validated for issuer, audience, expiry, and signing key?
- [ ] Is mutual TLS used for service-to-service communication?
- [ ] Are message queue publishers authenticated?
- [ ] Is session fixation prevented (new session ID after login)?

### Mitigations
- **Entra ID / OIDC** for interactive authentication
- **Managed Identity** for Azure service-to-service
- **Mutual TLS** for critical internal services
- **Message signing** (HMAC) for queue-based communication
- **Certificate pinning** for external payment gateway calls

## T — Tampering (Integrity Threats)

**Core Question:** Can an attacker modify data in transit or at rest?

### Threats by Component

| Component | Threat | Example |
|-----------|--------|---------|
| HTTP Request | Parameter manipulation | Changing escrow amount in POST body |
| Database | Data modification | SQL injection modifying escrow records |
| Config Files | Config tampering | Modified connection strings or feature flags |
| Log Files | Log manipulation | Attacker erasing evidence of access |
| API Response | Response injection | MITM modifying API response data |

### Detection Questions
- [ ] Is all data in transit encrypted with TLS 1.2+?
- [ ] Is input validation enforced server-side (not just client)?
- [ ] Are critical database operations logged in immutable audit trail?
- [ ] Are configuration files integrity-checked?
- [ ] Is anti-forgery (CSRF) protection enabled on state-changing endpoints?

### Mitigations
- **TLS everywhere** — HSTS enforced
- **FluentValidation** on all commands/inputs
- **Immutable audit log** (append-only table or Azure Table Storage)
- **Digital signatures** on financial transactions
- **EF Core parameterized queries** — never concatenate SQL

## R — Repudiation (Accountability Threats)

**Core Question:** Can a user deny performing an action?

### Key Scenarios for Escrow Platform
- Buyer denies initiating an escrow transaction
- Seller denies confirming delivery
- Admin denies modifying escrow terms
- User denies authorizing a fund release

### Mitigations
- **Structured audit logging** with user identity, timestamp, IP, correlation ID
- **Tamper-proof log storage** (Azure Monitor, immutable blob, or append-only DB)
- **Digital signatures** on escrow state transitions
- **Non-repudiation tokens** for high-value transactions
- **Correlation IDs** (`Activity.Current.Id`) across all distributed operations

```csharp
// Audit log entry for escrow operations
_auditLogger.LogEscrowAction(new AuditEntry
{
    UserId = currentUser.Id,
    Action = "EscrowReleased",
    EscrowId = escrow.Id,
    Amount = escrow.Amount,
    Timestamp = DateTimeOffset.UtcNow,
    IpAddress = httpContext.Connection.RemoteIpAddress,
    CorrelationId = Activity.Current?.Id
});
```

## I — Information Disclosure (Confidentiality Threats)

**Core Question:** Can an attacker access data they shouldn't see?

### Threats by Component

| Component | Threat | Example |
|-----------|--------|---------|
| API Response | Data leakage | Stack traces in error responses |
| Database | Data exposure | SQL injection extracting user records |
| Logs | PII in logs | Passwords, tokens, SSNs in log files |
| Client Storage | Client exposure | Sensitive data in localStorage |
| Config Files | Secret exposure | API keys in committed `appsettings.json` |
| HTTP Headers | Server info | `Server: Kestrel`, `X-Powered-By: ASP.NET` |

### Mitigations
- **Data classification** — label data as Public/Internal/Confidential/Restricted
- **Encrypt at rest** — TDE for SQL, Data Protection API for fields
- **Sanitize errors** — generic messages in production
- **Never log secrets/PII** — use structured logging with redaction
- **Remove server headers** — strip `Server`, `X-Powered-By`
- **Classify and control** — access controls per data classification

## D — Denial of Service (Availability Threats)

**Core Question:** Can an attacker make the system unavailable?

### Mitigations
- **Rate limiting** (`AddRateLimiter` in ASP.NET Core 7+)
- **Request size limits** (`MaxRequestBodySize`, file upload limits)
- **Query pagination** — never `ToListAsync()` unbounded
- **Circuit breakers** (Polly) on external service calls
- **Timeout policies** on all async operations
- **Bulkhead isolation** for critical vs. non-critical paths
- **Auto-scaling** with health probes

## E — Elevation of Privilege (Authorization Threats)

**Core Question:** Can an attacker gain higher privileges than intended?

### Mitigations
- **Policy-based authorization** — `[Authorize(Policy = "EscrowAdmin")]`
- **Resource-based auth** — verify ownership before access
- **IDOR prevention** — use GUIDs, filter by authenticated user
- **Safe deserialization** — no `BinaryFormatter`, no `TypeNameHandling.All`
- **Path validation** — prevent directory traversal
- **Least privilege** — Managed Identity with minimal permissions
