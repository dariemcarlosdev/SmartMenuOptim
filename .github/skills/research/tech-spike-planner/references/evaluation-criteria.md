# Evaluation Criteria

Structured criteria for evaluating technologies, libraries, and approaches.

## Standard Evaluation Dimensions

| Dimension | What to Assess | Weight Guide |
|-----------|---------------|--------------|
| **Functionality** | Does it solve the core problem? | Critical |
| **Performance** | Meets latency/throughput targets? | High |
| **Security** | OWASP compliance, vulnerability history? | High |
| **Integration** | Works with .NET 10, EF Core, Blazor? | High |
| **Maturity** | Stable releases, production-proven? | Medium |
| **Community** | Active maintainers, documentation quality? | Medium |
| **Licensing** | Compatible with commercial use? | Medium |
| **Cost** | Runtime, licensing, infrastructure costs? | Medium |
| **Complexity** | Learning curve, maintenance burden? | Medium |
| **Extensibility** | Can be customized for our needs? | Low |

## .NET Platform Criteria

When evaluating for the NexTruzt escrow platform:

### Must-Have Criteria (Non-Negotiable)

```
- [ ] Compatible with .NET 10 / ASP.NET Core
- [ ] Supports async/await patterns (CancellationToken propagation)
- [ ] Works with dependency injection (Microsoft.Extensions.DI)
- [ ] No GPL/AGPL licensing conflicts (MIT/Apache preferred)
- [ ] Active maintenance (commit within last 6 months)
- [ ] No known critical CVEs unpatched
```

### Should-Have Criteria (Preferred)

```
- [ ] NuGet package with stable versioning (not pre-release only)
- [ ] Works with EF Core 10
- [ ] Supports Blazor Server scenarios
- [ ] FluentValidation integration or compatible validation
- [ ] Structured logging support (ILogger<T>)
- [ ] OpenTelemetry/metrics instrumentation
```

## Performance Benchmarking Template

```csharp
// Minimal benchmark for .NET library evaluation
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class LibraryBenchmark
{
    [Benchmark(Baseline = true)]
    public async Task CurrentApproach()
    {
        // Current implementation
    }

    [Benchmark]
    public async Task CandidateA()
    {
        // Candidate library A
    }

    [Benchmark]
    public async Task CandidateB()
    {
        // Candidate library B
    }
}
```

### Performance Targets (Fintech)

| Metric | Acceptable | Target | Unacceptable |
|--------|-----------|--------|-------------|
| API response (P95) | < 500ms | < 200ms | > 1s |
| Throughput | > 100 req/s | > 500 req/s | < 50 req/s |
| Memory per request | < 10MB | < 2MB | > 50MB |
| Cold start | < 5s | < 2s | > 10s |

## Security Evaluation Checklist

```
- [ ] Check NVD/CVE databases for known vulnerabilities
- [ ] Review GitHub Security Advisories for the package
- [ ] Verify package signing and supply chain integrity
- [ ] Check for dependency vulnerabilities (dotnet list package --vulnerable)
- [ ] Review authentication/authorization integration patterns
- [ ] Assess data protection capabilities (encryption at rest/in transit)
- [ ] Check OWASP dependency-check results
```

## Scoring Guide

Use consistent scoring across all evaluations:

| Score | Meaning | Criteria |
|-------|---------|----------|
| 5 | Excellent | Exceeds requirements, production-proven |
| 4 | Good | Meets requirements with minor gaps |
| 3 | Acceptable | Meets minimum requirements |
| 2 | Weak | Significant gaps, workarounds needed |
| 1 | Poor | Fails requirements, blockers present |
| 0 | Disqualified | Non-negotiable criteria not met |

## Red Flags (Automatic Disqualifiers)

- No release in > 12 months with open critical issues
- License incompatible with commercial use
- Requires unsafe code or elevated privileges without justification
- No support for current .NET LTS or STS version
- Known unpatched security vulnerabilities
- Single maintainer with no succession plan for critical dependency
