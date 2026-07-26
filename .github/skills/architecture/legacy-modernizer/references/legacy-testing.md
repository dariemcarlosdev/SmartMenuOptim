# Legacy Testing Reference

> **Load when:** Writing characterization tests, golden master tests, or testing legacy code without unit tests.

## Characterization Tests

Characterization tests capture the **current behavior** of legacy code — even if that behavior is "wrong." They serve as a safety net during refactoring by detecting unintended changes.

### The Characterization Test Process

1. **Call the code** with a specific input
2. **Observe the output** (even if it seems wrong)
3. **Write a test that asserts the observed output**
4. **The test now locks in the existing behavior**
5. **If the test breaks during refactoring, you changed behavior**

### Example: Characterizing a Legacy Fee Calculator

```csharp
// Legacy code — no documentation, unclear rules
public class FeeCalculator
{
    public decimal Calculate(string type, decimal amount, bool isPremium)
    {
        if (isPremium)
            return amount * 0.015m;
        if (type == "international")
            return amount * 0.035m + 25;
        return amount * 0.025m;
    }
}

// Characterization tests — document ACTUAL behavior
public sealed class FeeCalculatorCharacterizationTests
{
    private readonly FeeCalculator _sut = new();

    [Theory]
    [InlineData("standard", 1000, false, 25.00)]      // 2.5%
    [InlineData("standard", 1000, true, 15.00)]        // 1.5% premium
    [InlineData("international", 1000, false, 60.00)]  // 3.5% + $25 flat
    [InlineData("international", 1000, true, 15.00)]   // Premium overrides international!
    [InlineData("standard", 0, false, 0)]              // Zero amount edge case
    [InlineData("unknown", 500, false, 12.50)]         // Unknown type gets default
    public void Calculate_MatchesLegacyBehavior(string type, decimal amount, bool premium, decimal expected)
    {
        var result = _sut.Calculate(type, amount, premium);
        Assert.Equal(expected, result);
    }
}
```

**Note:** The test for `("international", 1000, true, 15.00)` reveals that premium overrides international pricing. This might be a bug, but the characterization test documents it so we don't accidentally "fix" it during refactoring without a deliberate decision.

## Golden Master Testing

For complex outputs (HTML pages, reports, API responses), compare against a saved "golden" snapshot.

### Implementation with Verify

```csharp
// Using Verify (https://github.com/VerifyTests/Verify) for snapshot testing
[UsesVerify]
public sealed class EscrowReportGoldenMasterTests
{
    [Fact]
    public async Task GenerateReport_MatchesGoldenMaster()
    {
        var report = new LegacyReportGenerator();
        var escrows = GetSampleEscrows(); // Fixed test data

        var result = report.Generate(escrows);

        // First run: creates .verified.txt file (the golden master)
        // Subsequent runs: compares output against the golden master
        await Verify(result);
    }

    private static List<Escrow> GetSampleEscrows() =>
    [
        new Escrow { Id = "ESC-001", Amount = 5000m, Status = "Active" },
        new Escrow { Id = "ESC-002", Amount = 15000m, Status = "Pending" },
    ];
}
```

### Golden Master for Database Queries

```csharp
[Fact]
public async Task GetActiveEscrows_StoredProcedure_MatchesGoldenMaster()
{
    // Arrange — use a known database state
    await SeedTestDataAsync();

    // Act — call the legacy stored procedure
    var results = await _connection.QueryAsync<dynamic>(
        "EXEC sp_GetActiveEscrows @StatusCode = 'A', @MinAmount = 1000");

    // Assert — compare against golden master
    await Verify(results);
}
```

## Approval Testing

Similar to golden master but designed for human review of output changes:

```csharp
// Using ApprovalTests library
[Fact]
public void LegacyEmailTemplate_MatchesApproved()
{
    var generator = new LegacyEmailTemplateGenerator();
    var result = generator.GenerateEscrowConfirmation(
        buyerName: "Alice Johnson",
        sellerName: "Bob Smith",
        amount: 25000m,
        escrowId: "ESC-TEST-001");

    Approvals.Verify(result);
}
```

## Testing Legacy Code with No Tests

### Seam-Finding Technique

A "seam" is a place where you can alter behavior without editing the code. Find seams to make legacy code testable:

```csharp
// Original: Untestable — directly creates HttpClient
public class LegacyPaymentClient
{
    public string ProcessPayment(decimal amount)
    {
        var client = new HttpClient(); // No seam — can't mock
        var response = client.PostAsync("https://payments.example.com/charge",
            new StringContent($"amount={amount}")).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
}

// Extract seam: Make HttpClient injectable
public class LegacyPaymentClient
{
    private readonly HttpClient _client;

    // Object seam — inject dependency via constructor
    public LegacyPaymentClient(HttpClient? client = null)
    {
        _client = client ?? new HttpClient(); // Backward compatible
    }

    public string ProcessPayment(decimal amount)
    {
        var response = _client.PostAsync("https://payments.example.com/charge",
            new StringContent($"amount={amount}")).Result;
        return response.Content.ReadAsStringAsync().Result;
    }
}
```

### Sprout Method / Sprout Class

When legacy code is too risky to change, add new behavior in a separate testable method:

```csharp
// Legacy method — too complex and risky to modify
public void ProcessEscrow(EscrowRequest request)
{
    // 200 lines of tangled logic...
    // We need to add fee calculation but don't want to touch this method
}

// Sprout method — new behavior in a testable method
public decimal CalculateEscrowFee(EscrowRequest request)
{
    // New, clean, testable code
    ArgumentNullException.ThrowIfNull(request);
    return request.Amount * GetFeeRate(request.Type);
}

// Call the sprout from the legacy method with minimal change
public void ProcessEscrow(EscrowRequest request)
{
    // 200 lines of tangled logic...
    var fee = CalculateEscrowFee(request); // Single new line
    // Continue with fee...
}
```

## Integration Test Patterns for Legacy Systems

### Database-Backed Integration Tests

```csharp
// Test against a real database to verify legacy stored procedures
public sealed class LegacyStoredProcedureTests : IAsyncLifetime
{
    private readonly NpgsqlConnection _connection;

    public LegacyStoredProcedureTests()
    {
        _connection = new NpgsqlConnection(TestConfiguration.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        // Seed known test state
        await SeedTestDataAsync(_connection);
    }

    [Fact]
    public async Task sp_CalculateEscrowFees_ReturnsExpectedResults()
    {
        var result = await _connection.QueryFirstAsync<decimal>(
            "SELECT calculate_escrow_fee(@amount, @type)",
            new { amount = 10000m, type = "standard" });

        Assert.Equal(250m, result); // Lock in current behavior
    }

    public async Task DisposeAsync()
    {
        await CleanupTestDataAsync(_connection);
        await _connection.DisposeAsync();
    }
}
```

## Test Coverage Strategy for Legacy Code

Prioritize testing based on risk and change frequency:

```markdown
| Priority | Category | Strategy | Coverage Target |
|---|---|---|---|
| 1 | Code being modified | Characterization + unit tests | 80%+ |
| 2 | Critical business logic | Characterization + golden master | 70%+ |
| 3 | Integration boundaries | Integration tests | Key paths |
| 4 | Stable, rarely changed | Golden master only | Snapshot |
| 5 | Code being deleted | No new tests needed | — |
```
