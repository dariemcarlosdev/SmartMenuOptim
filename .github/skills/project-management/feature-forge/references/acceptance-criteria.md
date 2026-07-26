# Acceptance Criteria (Feature Forge)

Writing comprehensive acceptance criteria in Given/When/Then format.

## Given/When/Then Structure

```gherkin
Given {precondition — sets up the scenario}
When  {action — a single user or system action}
Then  {outcome — one or more verifiable results}
And   {additional outcome — extends Given, When, or Then}
```

## Coverage Matrix

Every feature must have criteria covering these categories:

| Category | Min. Criteria | Purpose |
|----------|-------------|---------|
| Happy path | 1-3 per story | Core functionality works |
| Validation | 1-2 per input | Invalid data is rejected |
| Authorization | 1 per role | Access control enforced |
| Error handling | 1-2 | Graceful failure |
| Edge cases | 1-2 | Boundary conditions |
| Audit | 1 | Compliance trail |

## NexTruzt Escrow Examples

### Happy Path Criteria

```gherkin
Scenario: Buyer creates a new escrow
  Given a verified buyer is authenticated
  And the buyer has a linked payment method
  When the buyer creates an escrow for $5,000 USD with seller "seller@example.com"
  Then a new escrow is created with status "Pending"
  And the seller receives an email notification
  And an audit entry is recorded with action "EscrowCreated"

Scenario: Both parties approve fund release
  Given an escrow exists with status "Funded"
  And the buyer has approved release
  When the seller confirms delivery
  Then the escrow status changes to "Released"
  And fund transfer is initiated within 5 seconds
```

### Validation Criteria

```gherkin
Scenario: Reject negative escrow amount
  Given a verified buyer is authenticated
  When the buyer creates an escrow with amount -100
  Then the system returns HTTP 400
  And the response contains error "Amount must be positive"
  And no escrow is created

Scenario: Reject unsupported currency
  Given a verified buyer is authenticated
  When the buyer creates an escrow with currency "XYZ"
  Then the system returns HTTP 400
  And the response contains error "Currency 'XYZ' is not supported"
```

### Authorization Criteria

```gherkin
Scenario: Non-participant cannot view escrow details
  Given an escrow exists between buyer "A" and seller "B"
  And user "C" is authenticated (not a participant)
  When user "C" requests the escrow details
  Then the system returns HTTP 403 Forbidden

Scenario: Only admin can override escrow timeout
  Given an expired escrow exists
  When a regular user attempts to extend the timeout
  Then the system returns HTTP 403 Forbidden
  When an admin extends the timeout
  Then the escrow deadline is updated
```

### Error Handling Criteria

```gherkin
Scenario: Payment gateway timeout
  Given a buyer is funding an escrow
  When the payment gateway does not respond within 30 seconds
  Then the system returns HTTP 503 with message "Payment service unavailable"
  And the escrow status remains "Pending" (not partially funded)
  And the system retries the payment after 5 minutes

Scenario: Concurrent modification
  Given two users attempt to approve the same escrow simultaneously
  When the second approval is processed
  Then the system returns HTTP 409 Conflict
  And the first approval is preserved
```

## Converting EARS to Acceptance Criteria

```
EARS: When a buyer creates an escrow, the system shall assign a unique EscrowId.

Acceptance Criteria:
  Given a verified buyer is authenticated
  When the buyer creates an escrow with valid data
  Then the response contains a unique EscrowId (UUID v4 format)
  And no two escrows share the same EscrowId
```

## Quality Checklist

Before finalizing acceptance criteria:

```
- [ ] Every user story has at least 1 happy path criterion
- [ ] Every input field has at least 1 validation criterion
- [ ] Every role has at least 1 authorization criterion
- [ ] At least 1 error handling criterion per external dependency
- [ ] All criteria use specific values (not "should work correctly")
- [ ] No implementation details in criteria (test WHAT, not HOW)
- [ ] Criteria are independently verifiable
```

## Anti-Patterns

| Anti-Pattern | Example | Fix |
|-------------|---------|-----|
| Vague outcome | "Then it works" | "Then status is 'Pending'" |
| Multiple actions | "When A and B and C" | Split into separate scenarios |
| Implementation leak | "Then save to SQL" | "Then escrow is persisted" |
| Missing error path | Only happy paths | Add validation + error scenarios |
| Untestable | "System is fast" | "Response within 200ms (P95)" |
