# Decision Matrix

Weighted decision matrices for structured technology evaluation.

## Decision Matrix Template

### Step 1: Define Criteria and Weights

```markdown
| Criterion      | Weight | Rationale                              |
|----------------|--------|----------------------------------------|
| Functionality  | 25%    | Must solve core problem                |
| Performance    | 20%    | Fintech latency requirements           |
| Security       | 20%    | OWASP compliance, regulatory           |
| Integration    | 15%    | .NET 10 / Blazor / EF Core fit         |
| Maturity       | 10%    | Production stability, community        |
| Cost           | 10%    | Licensing, infrastructure, maintenance |
| **Total**      | **100%** |                                      |
```

### Step 2: Score Each Option (1-5)

```markdown
| Criterion      | Weight | Option A | Option B | Option C |
|----------------|--------|----------|----------|----------|
| Functionality  | 25%    | 4        | 5        | 3        |
| Performance    | 20%    | 5        | 3        | 4        |
| Security       | 20%    | 4        | 4        | 5        |
| Integration    | 15%    | 5        | 3        | 4        |
| Maturity       | 10%    | 4        | 5        | 2        |
| Cost           | 10%    | 3        | 4        | 5        |
```

### Step 3: Calculate Weighted Scores

```markdown
| Criterion      | Weight | A (w)  | B (w)  | C (w)  |
|----------------|--------|--------|--------|--------|
| Functionality  | 0.25   | 1.00   | 1.25   | 0.75   |
| Performance    | 0.20   | 1.00   | 0.60   | 0.80   |
| Security       | 0.20   | 0.80   | 0.80   | 1.00   |
| Integration    | 0.15   | 0.75   | 0.45   | 0.60   |
| Maturity       | 0.10   | 0.40   | 0.50   | 0.20   |
| Cost           | 0.10   | 0.30   | 0.40   | 0.50   |
| **Total**      |        | **4.25**| **4.00**| **3.85**|
| **Rank**       |        | **1st** | **2nd** | **3rd** |
```

## NexTruzt Platform Weight Presets

### For Infrastructure Decisions

```
Security:      30%  (fintech regulatory requirements)
Performance:   25%  (transaction latency SLAs)
Functionality: 20%  (feature completeness)
Integration:   15%  (.NET ecosystem fit)
Cost:          10%  (operational budget)
```

### For Library/Package Selection

```
Integration:   25%  (.NET 10 / DI / async compatibility)
Functionality: 25%  (solves the problem)
Maturity:      20%  (stable, maintained, documented)
Security:      15%  (no CVEs, supply chain trust)
Performance:   15%  (meets latency targets)
```

### For Architecture Pattern Selection

```
Maintainability: 25%  (long-term team velocity)
Scalability:     20%  (growth trajectory)
Complexity:      20%  (team learning curve)
Testability:     15%  (automated testing support)
Performance:     10%  (runtime characteristics)
Migration:       10%  (effort to adopt from current state)
```

## Go/No-Go Decision Framework

After scoring, apply this decision logic:

```
IF any non-negotiable criterion scores 0 → DISQUALIFY
IF weighted total >= 4.0 → STRONG GO
IF weighted total 3.0-3.9 → CONDITIONAL GO (document risks)
IF weighted total 2.0-2.9 → WEAK — needs more investigation
IF weighted total < 2.0 → NO-GO
```

## Sensitivity Analysis

Test if the winner changes when weights shift:

```markdown
| Scenario            | Weights Changed       | Winner |
|--------------------|-----------------------|--------|
| Baseline           | As defined            | A      |
| Security-first     | Security +10%, Cost -10% | A   |
| Budget-constrained | Cost +10%, Performance -10% | B  |
| Speed-to-market    | Maturity +10%, Security -10% | B |
```

If the winner changes across scenarios, the decision is **sensitive** — document this and discuss with stakeholders.

## Architecture Decision Record (ADR)

After the matrix, capture the decision:

```markdown
# ADR-{NNN}: {Decision Title}

## Status: {Proposed | Accepted | Deprecated | Superseded}

## Context
{What prompted this decision? Link to spike document.}

## Decision
{We will use {Option A} because...}

## Consequences
### Positive
- {Benefit 1}

### Negative
- {Trade-off 1}

### Risks
- {Risk with mitigation}

## Alternatives Considered
| Option | Score | Reason Not Chosen |
|--------|-------|-------------------|
| B      | 4.00  | {Why rejected}    |
| C      | 3.85  | {Why rejected}    |
```

## Common Decision Anti-Patterns

| Anti-Pattern | Problem | Fix |
|-------------|---------|-----|
| Equal weights | No priorities expressed | Force-rank criteria |
| Score inflation | All 4s and 5s | Use full 1-5 range, anchor with examples |
| Missing criteria | Important factor ignored | Review with stakeholders before scoring |
| Single evaluator | Bias risk | Have 2-3 people score independently, average |
| No sensitivity check | Fragile decision | Vary weights ±10% and check if winner changes |
