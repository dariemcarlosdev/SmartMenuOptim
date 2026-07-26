# Prompt Optimization Reference

> **Load when:** Iteratively refining prompts, A/B testing variants, or reducing token consumption.

## Optimization Loop

```
Draft → Test → Measure → Identify Failures → Refine → Re-test
  ↑                                                       |
  └───────────────────────────────────────────────────────┘
```

## Token Reduction Strategies

### 1. Compress Instructions

| Before (verbose) | After (compressed) | Savings |
|---|---|---|
| "Please analyze the following transaction and provide a detailed assessment of whether it meets compliance requirements" | "Assess compliance for this transaction:" | ~60% |
| "You should respond in JSON format with the following fields: status, risk_level, and reasoning" | "Respond as JSON: {status, risk_level, reasoning}" | ~50% |
| "If you are unsure about the answer, please indicate that you are not confident" | "If unsure, set confidence: low" | ~65% |

### 2. Use References Instead of Inline Content

```text
# BAD — inline full file (~500 tokens)
Here is the EscrowService class:
```csharp
public sealed class EscrowService { ... 50 lines ... }
```

# GOOD — reference path (~20 tokens)
File: src/Domain/Services/EscrowService.cs (the agent has file access)
Focus: the ProcessRelease() method
```

### 3. Batch Few-Shot Examples

```text
# BAD — repetitive structure (~300 tokens per example)
Example 1: Input: "..." Output: {"type": "A"} Explanation: "..."
Example 2: Input: "..." Output: {"type": "B"} Explanation: "..."

# GOOD — tabular format (~150 tokens per example)
Examples:
| Input | Output | Explanation |
| "item never shipped" | {"type": "non_delivery"} | Missing shipment |
| "wrong color" | {"type": "not_as_described"} | Minor discrepancy |
```

### 4. Progressive Context Disclosure

Only include context the model actually needs for the current step.

```text
# BAD — full context upfront
Here is the entire escrow domain model, all entity configurations, the service layer...
Now answer: What is the status of escrow ESC-123?

# GOOD — minimal context
The escrow status enum has values: Pending, Funded, Released, Disputed, Cancelled.
Escrow ESC-123 was created 2024-01-15, funded 2024-01-16, dispute filed 2024-02-01.
Current status?
```

## A/B Testing Framework

### Test Design

```markdown
## Prompt Variant Test

**Objective:** Determine which prompt format produces more accurate escrow classifications
**Metric:** Accuracy on 20-case test suite
**Variants:**

| Variant | Change | Hypothesis |
|---|---|---|
| A (baseline) | Zero-shot with detailed instructions | Baseline accuracy |
| B | Add 3 few-shot examples | +15% accuracy on edge cases |
| C | CoT with step-by-step | +20% accuracy, +40% tokens |

**Test Suite:** 20 cases (10 clear, 5 edge, 5 adversarial)
```

### Statistical Significance

For meaningful A/B results:
- Minimum 20 test cases per variant
- Run each variant 3 times (account for model stochasticity)
- Use majority vote across runs
- Report confidence interval, not just average

## Common Anti-Patterns

| Anti-Pattern | Problem | Fix |
|---|---|---|
| Over-instructing | Redundant directives waste tokens | Remove instructions the model follows by default |
| Hedging language | "If possible, try to maybe consider..." adds noise | Use direct commands: "Classify as X" |
| Negative instructions only | "Don't do X, don't do Y" is less effective | State what TO do: "Always do Z" |
| Monolithic prompts | Single massive prompt for multi-step tasks | Chain smaller prompts |
| Example overkill | 10+ examples with diminishing returns | Use 3-5 diverse examples max |

## Prompt Version Control

```yaml
# prompt-manifest.yaml
prompts:
  escrow-classifier:
    version: "1.3.0"
    model: claude-sonnet-4
    avg_tokens: 450
    accuracy: 0.92
    last_tested: 2025-01-15
    changelog:
      - "1.3.0: Added structuring detection example"
      - "1.2.0: Compressed instructions (-30% tokens)"
      - "1.1.0: Added CoT for edge cases"
```

## Iterative Refinement Checklist

1. ☐ Run baseline prompt against full test suite
2. ☐ Identify top 3 failure categories
3. ☐ Draft one fix per failure category
4. ☐ Apply fixes and re-test
5. ☐ Measure token delta (ensure compression didn't increase cost)
6. ☐ Verify no regression on previously-passing cases
7. ☐ Document changes in prompt version history
