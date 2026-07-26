# Comment Anti-Patterns

Guide for identifying and avoiding poor commenting practices. Load this when reviewing comment quality.

## The Golden Rule

> Comment **WHY**, not **WHAT**. If the code needs a comment explaining WHAT it does, the code should be rewritten to be self-explanatory.

## Anti-Pattern Catalog

### 1. Stating the Obvious

The most common anti-pattern — comments that add zero information.

```csharp
// ❌ ANTI-PATTERN: Restating the code
var order = await repository.GetByIdAsync(orderId); // Get order by ID
customer.Name = request.Name; // Set customer name
if (order == null) return NotFound(); // Return not found if null
count++; // Increment count
var total = items.Sum(i => i.Price); // Calculate total price

// ✅ No comment needed — the code is self-explanatory
var order = await repository.GetByIdAsync(orderId);
customer.Name = request.Name;
if (order is null) return NotFound();
```

### 2. Journal Comments

Comments tracking change history — that's what git is for.

```csharp
// ❌ ANTI-PATTERN: Change log in code
// 2024-01-15: Added validation for negative amounts (John)
// 2024-02-01: Fixed bug where zero amount was allowed (Jane)
// 2024-03-10: Added currency validation (John)
public Result Validate(Money amount) { }

// ✅ Use git blame / git log for history
```

### 3. Commented-Out Code

Dead code disguised as comments — creates confusion about intent.

```csharp
// ❌ ANTI-PATTERN: Commented-out code
public async Task ProcessAsync()
{
    // var oldResult = await _legacyService.ProcessAsync();
    // if (oldResult.IsFailure)
    //     await _fallback.HandleAsync(oldResult.Error);
    var result = await _newService.ProcessAsync();
}

// ✅ Delete it — git has the history if you need it back
public async Task ProcessAsync()
{
    var result = await _newService.ProcessAsync();
}
```

### 4. Noise Comments

Comments required by misguided coding standards.

```csharp
// ❌ ANTI-PATTERN: Noise on every member
/// <summary>
/// Gets or sets the name.
/// </summary>
public string Name { get; set; }

/// <summary>
/// Default constructor.
/// </summary>
public Customer() { }

// ✅ Skip docs on self-explanatory members
// Only document when there's non-obvious behavior:
/// <summary>
/// Gets the customer's display name, falling back to email prefix when name is empty.
/// </summary>
public string DisplayName => string.IsNullOrWhiteSpace(Name)
    ? Email.Split('@')[0]
    : Name;
```

### 5. Misleading Comments

Comments that don't match the actual code behavior.

```csharp
// ❌ ANTI-PATTERN: Comment says one thing, code does another
// Returns null if not found
public Order GetOrder(int id) =>
    _orders.First(o => o.Id == id); // Actually throws if not found!

// ✅ Comment and code must agree
/// <exception cref="NotFoundException">Order does not exist.</exception>
public Order GetOrder(int id) =>
    _orders.FirstOrDefault(o => o.Id == id)
        ?? throw new NotFoundException(nameof(Order), id);
```

### 6. TODO Comments That Never Get Done

```csharp
// ❌ ANTI-PATTERN: Stale TODOs
// TODO: Add validation (added 2 years ago)
// HACK: Temporary fix for performance (been here 18 months)
// FIXME: This breaks when amount is zero

// ✅ Create a tracked issue instead
// If a TODO is truly needed short-term:
// TODO(#1234): Switch to batch processing after PaymentGateway v3 migration
```

## When Comments ARE Valuable

### Business Rules Not Obvious from Code
```csharp
// Escrow funds held for 24h after buyer confirmation (regulatory requirement, SEC Rule 15c3-3)
await HoldFundsAsync(escrow, TimeSpan.FromHours(24), ct);

// Fees waived for transactions under $10 per marketing promotion Q1-2025
if (amount < Money.USD(10)) return Money.Zero("USD");
```

### Workarounds with References
```csharp
// Workaround for EF Core bug #28571 — GroupBy with nullable navigation
// Remove after upgrading to EF Core 9.x
var results = await _context.Escrows
    .Where(e => e.Status != null)
    .GroupBy(e => e.Status!)
    .ToListAsync(ct);
```

### Performance Justifications
```csharp
// Using compiled regex — this runs in hot path (~10K invocations/sec)
private static readonly Regex EmailPattern =
    new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

// Pre-allocate to avoid list resizing — typical batch is 500-1000 items
var results = new List<EscrowDto>(capacity: 1024);
```

### Complex Algorithm Intent
```csharp
// Two-phase commit: first reserve funds in payment gateway,
// then persist escrow state. If persistence fails, release the reservation.
// This prevents orphaned fund holds.
```

## Review Checklist

When reviewing comments, ask:
1. Does this comment add information not available from the code?
2. Could the code be rewritten to eliminate the need for this comment?
3. Is the comment accurate (matches what the code actually does)?
4. Is this a stale TODO that should be a tracked issue?
5. Is this commented-out code that should be deleted?
