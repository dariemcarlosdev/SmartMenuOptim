# Feedback Examples

Guidelines and examples for writing constructive, actionable code review feedback.

## Feedback Principles

1. **Be specific** — Reference exact file, line, and code. Never say "this is bad."
2. **Explain why** — State the consequence, not just the rule violation.
3. **Suggest a fix** — Always include a concrete recommendation or code example.
4. **Acknowledge good work** — Positive feedback reinforces good patterns.
5. **Use severity consistently** — Follow the severity definitions strictly.
6. **Group patterns** — If the same issue appears 10 times, report it once with locations.

## Constructive Feedback Templates

### Security Finding
```
**Severity:** Critical | **Category:** Security/Injection
**File:** `src/Api/UserController.cs` | **Line:** 42-45

**Finding:** SQL string concatenation with user input creates SQL injection risk.
**Impact:** An attacker could extract, modify, or delete all database records.

**Current:**
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";

**Recommended:**
var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

**Why:** EF Core LINQ automatically parameterizes queries, eliminating injection risk.
```

### SOLID Violation
```
**Severity:** High | **Category:** SOLID/SRP
**File:** `src/Services/OrderService.cs` | **Lines:** 10-150

**Finding:** OrderService handles validation, payment processing, persistence,
and email notification — four distinct responsibilities.
**Impact:** Any change to validation logic risks breaking payment or notification flows.

**Recommended:** Extract into focused services:
- `OrderValidator` — business rule validation
- `IPaymentGateway` — payment processing (already exists as interface)
- `IOrderRepository` — persistence
- `INotificationService` — email dispatch
Orchestrate via MediatR `ProcessOrderCommandHandler`.
```

### Performance Issue
```
**Severity:** Medium | **Category:** Performance
**File:** `src/Queries/GetOrdersQuery.cs` | **Line:** 30

**Finding:** `Count() > 0` forces full enumeration of IEnumerable.
**Impact:** O(n) operation where O(1) is available.

**Current:** if (orders.Count() > 0)
**Recommended:** if (orders.Any())
**Why:** `Any()` short-circuits after the first element, avoiding full enumeration.
```

### Clean Code Issue
```
**Severity:** Medium | **Category:** Clean Code/Method Length
**File:** `src/Services/EscrowService.cs` | **Lines:** 45-130

**Finding:** `ProcessEscrow()` is 85 lines with 5 nested levels, handling
validation, state transitions, notifications, and audit logging.
**Impact:** Difficult to test individual behaviors; high cognitive load.

**Recommended:** Extract into focused private methods:
- `ValidateEscrowState()` — precondition checks
- `TransitionState()` — state machine logic
- `NotifyParties()` — notification dispatch
Main method becomes an orchestrator (~15 lines).
```

## Positive Feedback Examples

Positive observations are mandatory in every review. They reinforce good patterns:

```
✅ **Excellent use of value objects** — `Money`, `EscrowId`, and `Email` types
eliminate primitive obsession and encode domain rules at the type level.

✅ **Clean CQRS separation** — Commands and queries are properly separated
with focused MediatR handlers. The read models are well-optimized.

✅ **Thorough error handling** — External service calls are wrapped with
Polly retry policies and meaningful exception types.

✅ **Good test coverage** — The escrow state machine has comprehensive
Arrange-Act-Assert tests covering all valid transitions.
```

## Grouping Repeated Issues

When the same pattern appears multiple times, group it:

```
**Severity:** Medium | **Category:** Clean Code/Magic Numbers
**Pattern:** Hardcoded numeric values found in 8 locations

| File | Line | Value | Suggested Constant |
|------|------|-------|--------------------|
| `EscrowService.cs` | 42 | `30` | `EscrowTimeoutDays` |
| `EscrowService.cs` | 78 | `10000` | `HighValueThreshold` |
| `FeeCalculator.cs` | 15 | `0.025m` | `StandardFeeRate` |
| `FeeCalculator.cs` | 22 | `0.015m` | `DiscountedFeeRate` |

**Recommendation:** Extract to a `EscrowConstants` class or configuration via `IOptions<EscrowSettings>`.
```

## Tone Guidance

| ❌ Avoid | ✅ Prefer |
|----------|----------|
| "This is wrong" | "This could cause [specific issue]" |
| "You should know better" | "Consider using [pattern] because [reason]" |
| "This is a mess" | "This method has grown complex — extracting [X] would improve testability" |
| "Why didn't you use X?" | "Using [X] here would [benefit] because [reason]" |
| No feedback on good code | "Good use of [pattern] — this makes [benefit] clear" |
