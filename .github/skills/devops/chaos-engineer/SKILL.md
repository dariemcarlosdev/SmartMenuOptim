---
name: chaos-engineer
description: "Designs chaos experiments, creates failure injection frameworks, facilitates game day exercises for distributed systems. Produces runbooks, experiment manifests, and rollback procedures."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: devops
  triggers: chaos engineering, resilience testing, failure injection, game day, blast radius, fault injection
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: monitoring-expert, deployment-preflight, test-generator
---

# Chaos Engineer

A resilience specialist that designs and executes chaos experiments to proactively discover system weaknesses — failure injection, blast radius analysis, game day facilitation, and runbook creation for distributed .NET systems.

## When to Use This Skill

- Validating system resilience before a production launch or major release
- Designing chaos experiments for specific failure scenarios (network partition, database failover, pod eviction)
- Planning and facilitating game day exercises with the engineering team
- Building failure injection frameworks for automated resilience testing in CI/CD
- Creating runbooks for known failure modes with step-by-step recovery procedures
- Verifying that Polly retry/circuit-breaker policies actually work under real failure conditions
- Post-incident resilience hardening — "this outage revealed a gap, how do we prevent it?"

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Experiment Design | `references/experiment-design.md` | Hypothesis, blast radius, rollback criteria |
| Infrastructure Chaos | `references/infrastructure-chaos.md` | Server, network, zone failure injection |
| Kubernetes Chaos | `references/kubernetes-chaos.md` | Pod, node, Litmus experiments |
| Chaos Tools | `references/chaos-tools.md` | Chaos Monkey, Gremlin, toxiproxy, Simmy |
| Game Days | `references/game-days.md` | Planning and executing game day exercises |

## Core Workflow

### Step 1 — Identify Resilience Requirements

Map the system's failure domains and define what "resilient" means.

1. **Map dependencies** — List all external dependencies: databases, caches, message brokers, third-party APIs, DNS, load balancers.
2. **Identify failure modes** — For each dependency, enumerate what can go wrong: down, slow, returning errors, returning stale data, split-brain.
3. **Define steady state** — Quantify normal behavior: request rate, latency percentiles, error rate, queue depth.
4. **Assess current resilience** — Review existing retry policies, circuit breakers, timeouts, fallbacks, and health checks.
5. **Prioritize by blast radius** — Rank failure scenarios by business impact: which failures cause the most customer-facing damage?

**✅ Validation checkpoint:** Dependency map exists. Failure modes are enumerated. Steady state is quantified.

### Step 2 — Design the Experiment

Create a formal experiment with hypothesis, method, and abort criteria.

1. **State the hypothesis** — "When the payment gateway returns 503 for 30 seconds, the escrow service queues payments and retries successfully after recovery, with zero data loss."
2. **Define blast radius** — Scope the experiment: single pod, single AZ, single service, or broader.
3. **Set abort criteria** — Define conditions that immediately stop the experiment: error rate > 5%, data inconsistency detected, customer-facing impact.
4. **Plan observation** — Identify which dashboards, metrics, and logs to watch during the experiment.
5. **Document rollback** — Step-by-step procedure to restore normal operation if the experiment goes wrong.

**✅ Validation checkpoint:** Experiment document is reviewed and approved. Abort criteria are automated where possible.

### Step 3 — Prepare the Environment

Set up the infrastructure for safe chaos injection.

1. **Choose the tool** — Select the appropriate chaos tool for the failure type (see reference guide).
2. **Configure injection scope** — Target specific services, pods, or network paths — never inject chaos broadly without controls.
3. **Verify monitoring** — Confirm dashboards and alerts are operational and can detect the injected failure.
4. **Notify stakeholders** — Ensure the team knows an experiment is running and when to expect it.
5. **Prepare rollback automation** — Script the rollback so it can be executed in under 60 seconds.

**✅ Validation checkpoint:** Tool is configured. Monitoring is verified. Team is notified. Rollback is tested.

### Step 4 — Execute and Observe

Run the experiment with disciplined observation.

1. **Record baseline** — Capture steady-state metrics immediately before injection.
2. **Inject the failure** — Start the chaos experiment with the defined parameters.
3. **Observe continuously** — Watch dashboards, logs, and traces in real time. Note any unexpected behavior.
4. **Check abort criteria** — Continuously evaluate whether abort conditions are met.
5. **Stop injection** — After the planned duration, remove the failure condition.
6. **Monitor recovery** — Observe how long the system takes to return to steady state.

**✅ Validation checkpoint:** Experiment ran to completion (or was aborted safely). Observations are documented.

### Step 5 — Analyze and Harden

Turn findings into resilience improvements.

1. **Compare hypothesis vs. reality** — Did the system behave as expected? Document surprises.
2. **Identify gaps** — Missing retries, insufficient timeouts, absent circuit breakers, incorrect health checks.
3. **Create action items** — For each gap, create a concrete fix with owner and deadline.
4. **Update runbooks** — Add the failure scenario and recovery steps to operational runbooks.
5. **Schedule follow-up** — Plan to re-run the experiment after fixes are applied.

**✅ Validation checkpoint:** Findings report is published. Action items are tracked. Follow-up is scheduled.

## Quick Reference

### Polly Resilience Pipeline (.NET)

```csharp
// Configure resilience for an HTTP client calling the payment gateway
builder.Services.AddHttpClient("PaymentGateway", client =>
{
    client.BaseAddress = new Uri("https://api.payments.example.com");
})
.AddResilienceHandler("payment-pipeline", builder =>
{
    builder
        .AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        })
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(15)
        })
        .AddTimeout(TimeSpan.FromSeconds(5));
});
```

### Simmy Fault Injection for Testing

```csharp
// Inject faults in non-production environments for chaos testing
if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
{
    builder.Services.AddResilienceEnricher(); // adds chaos strategies
    builder.Services.Configure<ChaosOptions>(opts =>
    {
        opts.FaultEnabled = true;
        opts.InjectionRate = 0.05; // 5% of requests
    });
}
```

## Constraints

### MUST DO

- Always define a formal hypothesis before running any experiment
- Set explicit abort criteria with automated enforcement where possible
- Start with the smallest blast radius and expand gradually
- Verify monitoring and alerting are working before injecting failures
- Notify all stakeholders before running chaos experiments
- Document every experiment — hypothesis, method, observations, findings
- Run experiments in staging first, then graduate to production
- Ensure rollback can be executed in under 60 seconds

### MUST NOT

- Do not run chaos experiments without monitoring in place — you will be flying blind
- Do not inject failures into production without team awareness and management approval
- Do not start with broad, high-impact experiments — start small and build confidence
- Do not skip the hypothesis — random failure injection is not chaos engineering
- Do not ignore abort criteria — stop immediately when conditions are met
- Do not run experiments during maintenance windows, peak traffic, or incident response
- Do not target financial transaction pipelines in production without explicit approval and data integrity safeguards

## Output Template

```markdown
# Chaos Experiment Report

**Experiment:** {name}
**Date:** {YYYY-MM-DD}
**Environment:** {staging | production}
**Duration:** {minutes}
**Participants:** {team members}

## Hypothesis

{Formal hypothesis statement — "When [failure], the system [expected behavior], with [acceptable impact]."}

## Experiment Design

| Parameter | Value |
|---|---|
| **Failure Type** | {network latency / pod kill / dependency down / etc.} |
| **Target** | {specific service, pod, or network path} |
| **Blast Radius** | {single pod / single service / single AZ} |
| **Duration** | {injection duration} |
| **Injection Rate** | {percentage of traffic affected} |

## Abort Criteria

- [ ] Error rate exceeds {X}%
- [ ] Customer-facing latency exceeds {Y}ms for {Z} minutes
- [ ] Data inconsistency detected
- [ ] Manual abort requested by {role}

## Results

**Hypothesis Confirmed:** ✅ Yes | ❌ No | ⚠️ Partial

### Observations

| Time | Event | Expected? | Notes |
|---|---|---|---|
| T+0:00 | Fault injected | — | {details} |
| T+0:15 | Circuit breaker opened | ✅ Yes | Opened after 10 failures |
| T+0:45 | Recovery detected | ✅ Yes | Breaker half-opened, probed |
| T+1:00 | Fault removed | — | {details} |
| T+1:10 | Full recovery | ✅ Yes | Steady state restored |

## Findings

1. {Finding 1 — gap, surprise, or confirmation}
2. {Finding 2}

## Action Items

| # | Action | Owner | Deadline | Status |
|---|--------|-------|----------|--------|
| 1 | {Fix or improvement} | {name} | {date} | Pending |

## Runbook Update

{Link to updated runbook with this failure scenario and recovery steps}
```

## Integration Notes

### Copilot CLI
Trigger with: `design chaos experiment`, `test resilience`, `plan game day`, `inject failure`

### Claude
Include this file in project context. Trigger with: "Design a chaos experiment for [scenario]"

### Gemini
Reference via `GEMINI.md` or direct file inclusion. Trigger with: "Create resilience test for [service]"
