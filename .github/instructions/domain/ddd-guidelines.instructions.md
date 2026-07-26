---
applyTo: "EscrowApp/Models/**/*.cs, EscrowApp/Events/**/*.cs"
---

# Domain-Driven Design Guidelines — NexTruzt.io Escrow Domain

## Rich Domain Models

- `EscrowTransaction` is the **aggregate root** — all state mutations flow through its public methods.
- Encapsulate behavior inside the entity: `HoldFunds()`, `ReleaseFunds()`, `RaiseDispute()`, `Cancel()`.
- Never expose public setters. Use factory methods or constructors for creation, behavior methods for transitions.
- Guard every state transition with precondition checks — throw `DomainException` (or a typed subclass) when an invariant is violated.

```csharp
// ✅ Rich model — behavior lives on the entity
public void RaiseDispute(Actor initiator, string reason)
{
    if (Status is not EscrowStatus.FundsHeld)
        throw new InvalidEscrowStateException(Id, Status, EscrowStatus.FundsHeld);

    Status = EscrowStatus.Disputed;
    AddDomainEvent(new DisputeRaisedEvent(Id, initiator.Id, reason));
}

// ❌ Anemic — logic scattered across services
transaction.Status = EscrowStatus.Disputed; // bypasses invariants
```

## Value Objects

- Use Value Objects for concepts that have **no identity** — equality is based on structural value.
- Candidates in this domain: `Money` (amount + currency), `Currency`, `IdempotencyKey`, `WalletAddress`.
- Implement as `record` or `readonly struct` with self-validation in the constructor.
- Override equality/hash semantics (records do this automatically).

```csharp
public sealed record Money(decimal Amount, Currency Currency)
{
    public Money
    {
        if (Amount < 0) throw new ArgumentOutOfRangeException(nameof(Amount));
    }
}
```

## Aggregate Boundaries

- `EscrowTransaction` is the **sole aggregate root** for the escrow lifecycle.
- Child entities (`Actor`, milestone line items, etc.) are accessed **only** through the aggregate root — never loaded independently via a repository.
- Persist and load the entire aggregate in a single unit of work to maintain transactional consistency.
- Keep aggregates small — resist the urge to pull unrelated concepts (e.g., user profiles) inside the boundary.

## Domain Events

- Raise events **from within the aggregate** using a base-class `AddDomainEvent()` helper.
- Events are **past-tense facts**: `PaymentReceivedEvent`, `DisputeRaisedEvent`, `FundsReleasedEvent`.
- Events carry only the data needed by handlers — IDs and relevant state, never full entity graphs.
- Domain events must be **pure data** (no service dependencies, no async calls inside the event itself).
- Dispatch events **after** the aggregate is persisted (outbox pattern or EF Core `SaveChanges` interception) to avoid side effects on rollback.

```csharp
public sealed record PaymentReceivedEvent(
    Guid TransactionId,
    Money Amount,
    string PaymentIntentId) : IDomainEvent;
```

## Strategy Interfaces

- Strategy interfaces belong in the **Domain layer** — they define *what* the domain needs, not *how* it's fulfilled.
- `IFundHoldable` — authorize/hold funds from the buyer's payment source.
- `IFundReleasable` — release held funds to the seller upon fulfillment.
- `IFundCancellable` — void/refund held funds on cancellation or dispute resolution.
- Infrastructure provides concrete implementations (e.g., `StripeFundHoldStrategy`).
- The aggregate references strategies by interface; the Application layer injects the concrete implementation via DI.

```csharp
// Domain — pure interface
public interface IFundHoldable
{
    Task<FundHoldResult> HoldAsync(EscrowTransaction transaction, CancellationToken ct);
}
```

## Entity Invariants

- Validate **in the constructor** — an entity must never exist in an invalid state.
- Use guard clauses at the top of every public method that mutates state.
- Required fields are enforced at construction time, not by external validators.
- Status transitions follow an explicit state machine — document allowed transitions.

```
Created → FundsHeld → Released | Disputed | Cancelled
Disputed → Resolved → Released | Refunded
```

## Pure Domain — No Framework Dependencies

- Domain classes must be **plain C# POCOs**: no `[Table]`, `[Column]`, `[Required]`, or EF Core attributes.
- No references to MediatR, ASP.NET Core, Entity Framework, or any infrastructure NuGet package.
- Mapping to persistence is handled in the Infrastructure layer via Fluent API (`IEntityTypeConfiguration<T>`).
- Domain events implement a thin marker interface (`IDomainEvent`) defined in the Domain project — not `INotification` from MediatR.

## Actor Model

- `Actor` represents a **participant** in an escrow transaction (buyer, seller, arbitrator).
- An `Actor` is an entity within the `EscrowTransaction` aggregate — it has identity but is not a standalone aggregate root.
- Store the actor's role, display identity, and reference to their authentication identity.
- Actors are created and associated during transaction setup — never modified independently.

## IdentityMapping — Web2/Web3 Bridge

- `IdentityMapping` bridges a user's **Web2 identity** (Stripe customer ID, email) with their **Web3 identity** (wallet address, ENS name).
- Modeled as a standalone entity (or aggregate) outside the `EscrowTransaction` boundary — transactions reference it by ID.
- Ensure one-to-many mapping: a single user can have multiple wallet addresses but one primary Stripe identity.
- Validate wallet address format in the Value Object; validate Stripe customer ID existence at the Application layer.

## General Rules

- Prefer `Guid` for entity identifiers — generated at creation time, not by the database.
- Use `DateTimeOffset` for all timestamps — never `DateTime`.
- Collections exposed from aggregates must be `IReadOnlyCollection<T>` — mutation only through aggregate methods.
- All domain code must be **synchronous** — async belongs in Application and Infrastructure layers.
