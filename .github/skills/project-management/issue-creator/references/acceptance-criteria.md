# Acceptance Criteria (Issue-Level)

Writing testable acceptance criteria for GitHub issues.

## Format Options

### Given/When/Then (Preferred for Features)

```gherkin
Given {precondition}
When  {action}
Then  {expected outcome}
```

### Checkbox List (Preferred for Chores)

```markdown
- [ ] {Specific, verifiable condition}
- [ ] {Specific, verifiable condition}
```

## Writing Effective Criteria

### Feature Acceptance Criteria

```markdown
### Acceptance Criteria

**Happy Path:**
- [ ] Given a verified buyer, when they create an escrow with valid data,
      then a new escrow is created with status "Pending"

**Validation:**
- [ ] When amount is zero or negative, then return validation error
- [ ] When currency is not supported, then return validation error

**Authorization:**
- [ ] When an unauthenticated user attempts to create an escrow,
      then return 401 Unauthorized

**Error Handling:**
- [ ] When the database is unavailable, then return 503 Service Unavailable
      and log the error with correlation ID

**Audit:**
- [ ] When an escrow is created, then an audit log entry is recorded
      with user ID, timestamp, and action
```

### Bug Acceptance Criteria

```markdown
### Acceptance Criteria

- [ ] The bug no longer reproduces following the steps above
- [ ] A regression test covers this specific scenario
- [ ] The fix does not break existing escrow creation tests
- [ ] Error response returns proper validation message instead of 500
```

### Chore Acceptance Criteria

```markdown
### Acceptance Criteria

- [ ] All NuGet packages updated to latest stable versions
- [ ] dotnet build succeeds with zero warnings
- [ ] All existing tests pass (no regressions)
- [ ] No new security vulnerabilities introduced (dotnet list package --vulnerable)
```

## Coverage Categories

Every feature issue should have criteria covering:

| Category | Minimum Criteria | Example |
|----------|-----------------|---------|
| Happy path | 1-3 | "Creates escrow with correct status" |
| Validation | 1-2 per input field | "Rejects negative amount" |
| Authorization | 1 per role | "Admin can access, buyer cannot" |
| Error handling | 1-2 | "Returns 503 when DB unavailable" |
| Edge cases | 1-2 | "Handles concurrent requests" |

## Quality Gate

Before submitting an issue, verify acceptance criteria pass this gate:

```
✅ SPECIFIC: Names exact values, status codes, error messages
   Bad:  "Shows error"
   Good: "Returns HTTP 400 with message 'Amount must be positive'"

✅ TESTABLE: Someone can write a test for this criterion
   Bad:  "Works correctly"
   Good: "Given amount=$100, when escrow created, then status='Pending'"

✅ INDEPENDENT: Each criterion can be verified separately
   Bad:  "Steps 1-5 work"
   Good: Each step is its own criterion

✅ COMPLETE: Covers happy path + at least 1 error path
   Bad:  Only happy path criteria
   Good: Happy path + validation + auth + error handling
```

## Mapping Criteria to Tests

| Criteria Pattern | Test Type | .NET Implementation |
|-----------------|-----------|-------------------|
| "Given...When...Then" | Integration | `WebApplicationFactory` + xUnit |
| "Returns HTTP {code}" | Integration | `HttpClient.SendAsync` assertion |
| "Validation error" | Unit | FluentValidation test |
| "Audit log recorded" | Integration | Verify audit table entry |
| "No regression" | Full suite | `dotnet test` passes |

### Example Test from Criteria

```csharp
// Criteria: "Given a verified buyer, when they create an escrow
// with valid data, then a new escrow is created with status Pending"

[Fact]
public async Task CreateEscrow_WithValidData_ReturnsCreatedWithPendingStatus()
{
    // Given
    var client = _factory.CreateAuthenticatedClient(Role.Buyer);
    var request = new CreateEscrowRequest(Amount: 5000, Currency: "USD");

    // When
    var response = await client.PostAsJsonAsync("/api/escrows", request);

    // Then
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var escrow = await response.Content.ReadFromJsonAsync<EscrowResponse>();
    escrow!.Status.Should().Be("Pending");
}
```
