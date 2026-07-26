# EARS Syntax

Easy Approach to Requirements Syntax (EARS) for unambiguous requirements.

## EARS Patterns

EARS provides 5 sentence templates that eliminate ambiguity.

### 1. Ubiquitous (Always Active)

```
The <system> shall <action>.
```

**Use when:** The requirement is always active, unconditionally.

```
The escrow service shall encrypt all financial data at rest using AES-256.
The API shall include a correlation ID in every HTTP response header.
The system shall log all authentication attempts.
```

### 2. Event-Driven (Triggered by Event)

```
When <trigger>, the <system> shall <action>.
```

**Use when:** A specific event triggers the behavior.

```
When a buyer creates an escrow, the system shall generate a unique escrow ID.
When an escrow reaches its expiration date, the system shall change its status to "Expired".
When a payment gateway returns a timeout, the system shall retry the request up to 3 times.
```

### 3. State-Driven (Active While in State)

```
While <state>, the <system> shall <action>.
```

**Use when:** Behavior is active only during a specific system state.

```
While an escrow has status "Funded", the system shall prevent modification of the amount.
While the payment gateway is unavailable, the system shall queue outgoing transactions.
While the user session is active, the system shall refresh the auth token every 15 minutes.
```

### 4. Unwanted Behavior (Error/Exception Handling)

```
If <unwanted condition>, then the <system> shall <action>.
```

**Use when:** Specifying how the system handles errors or exceptional conditions.

```
If the database connection is lost, then the system shall return HTTP 503 and retry with exponential backoff.
If an escrow amount exceeds $100,000, then the system shall require admin approval before processing.
If a user provides an invalid CSRF token, then the system shall reject the request with HTTP 403.
```

### 5. Optional Feature

```
Where <feature is active>, the <system> shall <action>.
```

**Use when:** The behavior depends on a feature flag or configuration.

```
Where multi-currency support is enabled, the system shall convert amounts using the daily exchange rate.
Where two-factor authentication is required, the system shall prompt for a verification code after password entry.
```

## Compound EARS (Combining Patterns)

```
While <state>, when <trigger>, the <system> shall <action>.
```

```
While an escrow has status "Funded", when both buyer and seller approve release, the system shall initiate fund transfer within 5 seconds.

While the system is in maintenance mode, when a user attempts to create an escrow, the system shall display a maintenance notification and reject the request.
```

## EARS for NexTruzt Escrow Requirements

### Escrow Lifecycle

```
When a buyer submits a valid escrow request, the system shall create an escrow with status "Pending".
When a buyer deposits funds matching the escrow amount, the system shall change escrow status to "Funded".
While an escrow has status "Funded", when the buyer approves release, the system shall record buyer approval.
While an escrow has status "Funded" and both parties have approved, the system shall transfer funds to the seller within 24 hours.
If a fund transfer fails, then the system shall retain the funds in escrow and notify the operations team.
```

### Security Requirements

```
The system shall authenticate all API requests using JWT bearer tokens.
When a user fails authentication 5 times within 10 minutes, the system shall lock the account for 30 minutes.
If a request lacks a valid authorization token, then the system shall return HTTP 401.
The system shall hash all passwords using bcrypt with a minimum work factor of 12.
```

## EARS Quality Checklist

- [ ] Each requirement uses exactly one EARS pattern (or a valid compound)
- [ ] "Shall" is used (not "should", "may", "might", "could")
- [ ] The system actor is explicitly named
- [ ] The action is specific and measurable
- [ ] No implementation details (HOW) — only behavior (WHAT)
- [ ] Triggers and states are observable/testable

## Common Mistakes

| Mistake | Example | Fix |
|---------|---------|-----|
| Vague action | "shall handle errors" | "shall return HTTP 500 with error code" |
| Missing trigger | "shall send notification" | "When escrow is funded, shall send notification" |
| Using "should" | "should validate input" | "shall validate input" — shall = mandatory |
| Implementation detail | "shall use Redis for caching" | "shall cache query results for 5 minutes" |
| Compound without clarity | "shall do A and B" | Split into two requirements |
