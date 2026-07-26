# EARS Syntax (Feature Forge)

Easy Approach to Requirements Syntax for writing unambiguous requirements.

## The 5 EARS Patterns

### 1. Ubiquitous — Always active, no trigger

```
The <system> shall <action>.
```

```
The escrow service shall encrypt all PII at rest using AES-256.
The API shall validate all input using FluentValidation before processing.
The system shall log every state transition with a correlation ID.
```

### 2. Event-Driven — Triggered by a specific event

```
When <trigger>, the <system> shall <action>.
```

```
When a buyer creates an escrow, the system shall assign a unique EscrowId.
When both parties approve release, the system shall initiate fund transfer.
When a payment fails, the system shall send a failure notification to the buyer.
```

### 3. State-Driven — Active only while in a state

```
While <state>, the <system> shall <action>.
```

```
While an escrow is in "Funded" status, the system shall prevent amount modification.
While the payment gateway is offline, the system shall queue transactions.
While an admin is reviewing a dispute, the system shall lock the escrow.
```

### 4. Unwanted Behavior — Error/exception handling

```
If <unwanted condition>, then the <system> shall <action>.
```

```
If the user provides an expired token, then the system shall return 401 Unauthorized.
If the escrow amount exceeds the daily limit, then the system shall require admin approval.
If a concurrent modification is detected, then the system shall return 409 Conflict.
```

### 5. Optional Feature — Depends on configuration

```
Where <feature is active>, the <system> shall <action>.
```

```
Where multi-currency is enabled, the system shall convert using the daily exchange rate.
Where two-factor auth is required, the system shall prompt for a verification code.
```

## Compound Patterns

Combine state + event for complex behaviors:

```
While <state>, when <trigger>, the <system> shall <action>.
```

```
While escrow is "Funded", when the buyer requests cancellation,
the system shall initiate a refund workflow.

While the system is in read-only mode, when a user attempts a write operation,
the system shall return 503 with a retry-after header.
```

## Requirements Elicitation → EARS Conversion

### From User Story to EARS

```
User Story:
  As a buyer, I want to cancel a pending escrow so that I can recover my funds.

EARS Requirements:
  REQ-1: While escrow has status "Pending", when the buyer requests cancellation,
         the system shall change status to "Cancelled".
  REQ-2: When an escrow is cancelled, the system shall initiate a full refund
         within 24 hours.
  REQ-3: If a refund fails, then the system shall notify the operations team
         and retry after 1 hour.
```

### From Interview Notes to EARS

```
Stakeholder said: "We need to make sure nobody can mess with a funded escrow"

EARS Requirements:
  REQ-1: While escrow has status "Funded", the system shall reject all
         modification requests except status transitions.
  REQ-2: The system shall log all rejected modification attempts
         with user ID and timestamp.
```

## EARS Quality Checklist

```
- [ ] Uses "shall" (mandatory), not "should" (optional) or "may" (permitted)
- [ ] One requirement per sentence
- [ ] System actor is explicitly named
- [ ] Action is specific and measurable
- [ ] No implementation details (WHAT, not HOW)
- [ ] Trigger/state is observable and testable
- [ ] Numbered for traceability (REQ-001, REQ-002)
```

## Mapping EARS to Clean Architecture

| EARS Pattern | Typically Implemented In |
|-------------|------------------------|
| Ubiquitous | Cross-cutting: middleware, pipeline behaviors |
| Event-Driven | Application: MediatR command/query handlers |
| State-Driven | Domain: entity state guards, invariants |
| Unwanted Behavior | Application: validators, exception handlers |
| Optional Feature | Infrastructure: feature flags + configuration |
