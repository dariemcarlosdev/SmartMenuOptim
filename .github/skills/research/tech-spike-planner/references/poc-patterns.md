# Proof-of-Concept Patterns

Patterns for building focused, time-boxed proof-of-concepts.

## PoC Principles

1. **Prove one thing** — Each PoC answers a specific question
2. **Throwaway code** — Never promote PoC code to production
3. **Happy path only** — Error handling is out of scope
4. **Document findings** — The PoC is worthless without written conclusions
5. **Time-boxed** — Stop when time expires, even if incomplete

## PoC Project Structure (.NET)

```
Spike.{TopicName}/
├── Program.cs              # Entry point, minimal setup
├── Spike.{TopicName}.csproj
├── README.md               # Findings and conclusions
├── Scenarios/
│   ├── Scenario1.cs        # First question tested
│   └── Scenario2.cs        # Second question tested
└── Results/
    └── benchmark-results.md
```

### Minimal PoC Starter

```csharp
// Program.cs — keep it simple
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register only what's needed for the spike
builder.Services.AddDbContext<SpikeDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("SpikeDb")));

var app = builder.Build();

// Run the scenario
Console.WriteLine("=== Spike: {Topic} ===");
using var scope = app.Services.CreateScope();
var scenario = new Scenario1(scope.ServiceProvider);
await scenario.RunAsync();
Console.WriteLine("=== Complete ===");
```

## Common PoC Scenarios

### Integration Feasibility

Proves: "Can system A talk to system B?"

```csharp
public class IntegrationScenario
{
    public async Task RunAsync()
    {
        // 1. Configure connection
        var client = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

        // 2. Authenticate
        var token = await GetTokenAsync(client);

        // 3. Make the critical call
        var response = await client.GetAsync("/api/resource");

        // 4. Log result
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Body: {await response.Content.ReadAsStringAsync()}");

        // FINDING: Document if it works and any gotchas
    }
}
```

### Performance Comparison

Proves: "Is option A faster than option B?"

```csharp
public class PerformanceScenario
{
    public async Task RunAsync()
    {
        const int iterations = 1000;
        var sw = Stopwatch.StartNew();

        // Option A
        for (int i = 0; i < iterations; i++)
            await OptionA();
        var timeA = sw.Elapsed;

        sw.Restart();

        // Option B
        for (int i = 0; i < iterations; i++)
            await OptionB();
        var timeB = sw.Elapsed;

        Console.WriteLine($"Option A: {timeA.TotalMilliseconds:F2}ms total");
        Console.WriteLine($"Option B: {timeB.TotalMilliseconds:F2}ms total");
        Console.WriteLine($"Winner: {(timeA < timeB ? "A" : "B")}");
    }
}
```

### Architecture Validation

Proves: "Does this pattern work for our use case?"

```csharp
// Test if CQRS + Event Sourcing fits escrow workflows
public class ArchitectureScenario
{
    public async Task RunAsync()
    {
        // 1. Create aggregate
        var escrow = new EscrowAggregate();
        escrow.Create(buyerId, sellerId, amount);

        // 2. Apply domain events
        escrow.FundDeposited(amount);
        escrow.BuyerApproved();
        escrow.SellerConfirmed();

        // 3. Verify state reconstruction from events
        var events = escrow.GetUncommittedEvents();
        var rebuilt = EscrowAggregate.ReplayFrom(events);

        Console.WriteLine($"Events: {events.Count}");
        Console.WriteLine($"Final Status: {rebuilt.Status}");
        Console.WriteLine($"States match: {escrow.Status == rebuilt.Status}");
    }
}
```

## PoC Findings Template

```markdown
## PoC Results: {Topic}

**Date:** {YYYY-MM-DD}
**Time spent:** {hours} of {time-box} allocated
**Question:** {The specific question this PoC answers}

### Result: {PASS | FAIL | PARTIAL}

### Key Findings
1. {Finding with evidence}
2. {Finding with evidence}

### Gotchas Discovered
- {Unexpected behavior or limitation}

### Recommendation
{Go / No-Go / Needs further investigation}

### If Adopted — Next Steps
- [ ] {What production implementation would require}
```

## Anti-Patterns to Avoid

| Anti-Pattern | Problem | Instead |
|-------------|---------|---------|
| Gold-plating the PoC | Wastes time-box on polish | Happy path only |
| No written findings | Knowledge lost | Always write README |
| Promoting PoC code | Tech debt from day 1 | Rewrite for production |
| Unbounded scope | Never finishes | One question per PoC |
| No baseline comparison | Can't judge results | Always measure current state first |
