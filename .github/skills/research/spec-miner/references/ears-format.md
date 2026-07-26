# EARS Format (Spec-Miner)

Converting discovered code patterns into EARS-format requirements.

## Code-to-EARS Conversion Patterns

### Entity → Ubiquitous Requirements

When you find a domain entity, extract its invariants:

```csharp
// Found in code:
public sealed class Escrow : AggregateRoot
{
    public Money Amount { get; private set; }  // private set = immutable after creation
    public EscrowStatus Status { get; private set; }

    public Escrow(BuyerId buyer, SellerId seller, Money amount)
    {
        Guard.Against.NegativeOrZero(amount.Value, nameof(amount));
        Guard.Against.Null(buyer, nameof(buyer));
        Status = EscrowStatus.Pending;
    }
}
```

```
Discovered EARS Requirements:
  REQ-001: The system shall require a positive amount for escrow creation.
  REQ-002: The system shall require a buyer and seller for escrow creation.
  REQ-003: When an escrow is created, the system shall set initial status to "Pending".
  REQ-004: While an escrow exists, the system shall prevent direct modification of the amount.
```

### Validator → Event-Driven + Unwanted Behavior

```csharp
// Found in code:
public sealed class CreateEscrowValidator : AbstractValidator<CreateEscrowCommand>
{
    public CreateEscrowValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive");
        RuleFor(x => x.Amount).LessThanOrEqualTo(100_000).WithMessage("Amount exceeds limit");
        RuleFor(x => x.Currency).Must(c => SupportedCurrencies.Contains(c));
    }
}
```

```
Discovered EARS Requirements:
  REQ-005: If the escrow amount is zero or negative, then the system shall
           reject the request with error "Amount must be positive".
  REQ-006: If the escrow amount exceeds $100,000, then the system shall
           reject the request with error "Amount exceeds limit".
  REQ-007: If the currency is not in the supported list, then the system
           shall reject the request with a validation error.
```

### State Machine → State-Driven Requirements

```csharp
// Found in code:
public enum EscrowStatus
{
    Pending,    // Created, awaiting funding
    Funded,     // Payment received
    Approved,   // Both parties approved
    Released,   // Funds transferred
    Disputed,   // Under investigation
    Expired,    // Timed out
    Cancelled   // Manually cancelled
}
```

```
Discovered State Machine:
  [Pending] → [Funded]    (deposit received)
  [Funded]  → [Approved]  (both parties approve)
  [Approved]→ [Released]  (funds transferred)
  [Funded]  → [Disputed]  (party raises dispute)
  [Pending] → [Expired]   (timeout)
  [Pending] → [Cancelled] (buyer cancels)

EARS Requirements:
  REQ-008: While escrow is "Pending", when a deposit matching the amount is received,
           the system shall change status to "Funded".
  REQ-009: While escrow is "Funded", when both parties approve release,
           the system shall change status to "Approved".
  REQ-010: While escrow is "Released", the system shall prevent any status changes.
```

### Handler → Use Case Requirements

```csharp
// Found in code:
public sealed class ReleaseEscrowHandler : IRequestHandler<ReleaseEscrowCommand, Result>
{
    public async Task<Result> Handle(ReleaseEscrowCommand request, CancellationToken ct)
    {
        var escrow = await _repo.GetByIdAsync(request.EscrowId, ct);
        if (escrow is null) return Result.NotFound();
        if (escrow.Status != EscrowStatus.Approved) return Result.Invalid("Not approved");

        escrow.Release();
        await _paymentService.TransferAsync(escrow.Amount, escrow.SellerId, ct);
        await _repo.UpdateAsync(escrow, ct);
        return Result.Success();
    }
}
```

```
Discovered EARS Requirements:
  REQ-011: When an approved escrow release is requested, the system shall
           transfer funds to the seller.
  REQ-012: If a release is requested for a non-existent escrow, then the
           system shall return Not Found.
  REQ-013: If a release is requested for an escrow not in "Approved" status,
           then the system shall reject with "Not approved".
```

### Authorization → Security Requirements

```csharp
// Found in code:
[Authorize(Policy = "EscrowParticipant")]
public async Task<IActionResult> GetEscrow(Guid id) { }

[Authorize(Roles = "Admin")]
public async Task<IActionResult> ResolveDispute(Guid id) { }
```

```
Discovered EARS Requirements:
  REQ-014: The system shall restrict escrow detail access to participants
           (buyer, seller) and administrators.
  REQ-015: The system shall restrict dispute resolution to administrators only.
```

## Confidence Levels for Discovered Requirements

| Confidence | Source | Action |
|-----------|--------|--------|
| **High** | Validator rule + test covering it | Document as-is |
| **Medium** | Handler logic without explicit test | Document + flag for validation |
| **Low** | Inferred from naming/structure | Document as "suspected" + investigate |

## Output Numbering Convention

```
REQ-{NNN}  — Functional requirement
NFR-{NNN}  — Non-functional requirement
SEC-{NNN}  — Security requirement
INT-{NNN}  — Integration requirement
```
