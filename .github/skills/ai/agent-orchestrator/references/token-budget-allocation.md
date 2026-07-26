# Token Budget Allocation Reference

> **Load when:** Planning fleet-wide token usage, estimating costs, or optimizing agent count.

## Token Budget by Agent Type

| Agent Type | Model | Typical Prompt | Typical Response | Total Per Agent | Cost Tier |
|---|---|---|---|---|---|
| `explore` | Haiku | ~5K input | ~15K output | ~20K total | Low ($) |
| `task` | Haiku | ~3K input | ~12K output | ~15K total | Low ($) |
| `general-purpose` | Sonnet | ~15K input | ~35K output | ~50K total | Medium ($$) |
| `critic` | Sonnet | ~10K input | ~20K output | ~30K total | Medium ($$) |

## Budget Allocation Formula

### Step 1: Calculate Total Available Budget

```
Total Budget = Context Window Size × 0.8 (safety margin)

For Sonnet (200K window):  200K × 0.8 = 160K usable
For Haiku (200K window):   200K × 0.8 = 160K usable
```

### Step 2: Assign Complexity Weights

| Complexity | Weight | Description | Example |
|---|---|---|---|
| S (Small) | 1 | Single file scan, simple lookup | "Find all usages of IEscrowRepository" |
| M (Medium) | 2 | Multi-file analysis, pattern detection | "Analyze auth module for security issues" |
| L (Large) | 3 | Multi-step implementation, refactoring | "Implement escrow release handler" |
| XL (Extra Large) | 5 | Cross-cutting changes, architecture work | "Redesign the payment processing pipeline" |

### Step 3: Allocate Per-Agent Budget

```
Total Work Weight = Sum of all unit weights
Reserve = Total Budget × 0.20 (for aggregation + follow-up + error recovery)
Available = Total Budget - Reserve

Per-unit Budget = Available × (Unit Weight / Total Work Weight)
```

### Example Calculation

```
Task: Analyze and implement escrow release feature

Work Units:
  1. Explore domain model (M, weight=2, explore agent)
  2. Explore infrastructure (M, weight=2, explore agent)
  3. Critic review plan (M, weight=2, critic agent)
  4. Implement handler (L, weight=3, general-purpose agent)
  5. Run tests (S, weight=1, task agent)

Total Weight = 2 + 2 + 2 + 3 + 1 = 10
Total Budget = 160K tokens
Reserve (20%) = 32K tokens
Available = 128K tokens

Budget per unit:
  1. Explore domain:  128K × (2/10) = 25.6K → fits explore (~20K) ✅
  2. Explore infra:   128K × (2/10) = 25.6K → fits explore (~20K) ✅
  3. Critic review:   128K × (2/10) = 25.6K → fits critic (~30K) ⚠️ tight
  4. Implement:       128K × (3/10) = 38.4K → fits general-purpose (~50K) ⚠️ tight
  5. Run tests:       128K × (1/10) = 12.8K → fits task (~15K) ✅
```

## Awareness Thresholds

Track token consumption and take action at these thresholds:

| Threshold | % Used | Action |
|---|---|---|
| Green | 0-50% | Normal operation, proceed as planned |
| Yellow | 50-70% | Review remaining units — can any be merged or simplified? |
| Orange | 70-85% | Reduce context in remaining agents, skip non-essential units |
| Red | 85-95% | Finish current agents, do remaining work yourself |
| Critical | 95%+ | Stop all agents, aggregate what you have, report partial results |

## Cost-Per-Action Estimates

| Action | Input Tokens | Output Tokens | Total |
|---|---|---|---|
| Read a single file (200 lines) | 1,500 | 0 | 1,500 |
| Grep search with results | 500 | 2,000 | 2,500 |
| Write a handler + validator (~100 lines) | 3,000 | 5,000 | 8,000 |
| Review a diff (50 lines changed) | 4,000 | 3,000 | 7,000 |
| Run build + capture output | 500 | 5,000 | 5,500 |
| Run test suite + capture output | 500 | 8,000 | 8,500 |
| Synthesize 3 agent reports | 10,000 | 5,000 | 15,000 |

## Fleet Size Guidelines

| Fleet Size | Scenario | Token Overhead | Recommendation |
|---|---|---|---|
| 1 agent | Simple delegation | Minimal (~5K overhead) | Use when task is complex but single-threaded |
| 2-3 agents | Standard parallelism | Moderate (~15K overhead) | Most common — good cost/speed balance |
| 4-5 agents | Heavy parallelism | Significant (~30K overhead) | Large codebase analysis, multi-module changes |
| 6+ agents | Extreme parallelism | High (~50K+ overhead) | Rarely justified — batch in waves of 5 |

**Overhead includes:** Common preamble per agent, tracking queries, result collection, aggregation.

## Budget Optimization Strategies

### 1. Merge Small Units

```
# Before: 4 agents
  Agent 1: Check file A (S, 15K)
  Agent 2: Check file B (S, 15K)
  Agent 3: Check file C (S, 15K)
  Agent 4: Check file D (S, 15K)
  Total: 60K tokens + 20K overhead = 80K

# After: 1 agent checking all 4 files
  Agent 1: Check files A, B, C, D (M, 25K)
  Total: 25K tokens + 5K overhead = 30K
  Savings: 50K tokens (62% reduction)
```

### 2. Use Explore Instead of General-Purpose

```
# Before: general-purpose agent for read-only analysis (50K)
# After: explore agent for the same task (20K)
# Savings: 30K tokens per agent
```

### 3. Progressive Prompting

```
# Before: Include everything upfront (10K prompt)
# After: Start with 3K, add 2K if agent needs more
# Savings: 5K tokens on average (many agents don't need all context)
```

## Token Tracking SQL

```sql
-- Create tracking table
CREATE TABLE token_usage (
    agent_id TEXT PRIMARY KEY,
    agent_type TEXT NOT NULL,
    budget_tokens INTEGER NOT NULL,
    actual_tokens INTEGER DEFAULT 0,
    status TEXT DEFAULT 'pending'
);

-- Insert budget plan
INSERT INTO token_usage (agent_id, agent_type, budget_tokens) VALUES
    ('explore-domain', 'explore', 20000),
    ('explore-infra', 'explore', 20000),
    ('critic-plan', 'critic', 30000),
    ('implement-handler', 'general-purpose', 50000);

-- Check total budget usage
SELECT 
    SUM(budget_tokens) as total_budget,
    SUM(actual_tokens) as total_used,
    ROUND(CAST(SUM(actual_tokens) AS FLOAT) / SUM(budget_tokens) * 100, 1) as pct_used
FROM token_usage;
```
