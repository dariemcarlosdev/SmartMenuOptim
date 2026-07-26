# Prompt Patterns Reference

> **Load when:** Selecting zero-shot, few-shot, chain-of-thought, or ReAct patterns for a prompt.

## Pattern Selection Matrix

| Pattern | Best For | Token Cost | Accuracy | Complexity |
|---|---|---|---|---|
| Zero-Shot | Simple classification, formatting, translation | Low | Medium | Low |
| Few-Shot | Domain-specific tasks, consistent formatting | Medium | High | Medium |
| Chain-of-Thought (CoT) | Multi-step reasoning, math, logic | Medium-High | Very High | Medium |
| ReAct | Tool-using agents, dynamic decision-making | High | Very High | High |
| Self-Consistency | Critical decisions requiring confidence scoring | Very High | Highest | High |

## Zero-Shot Pattern

Direct instruction with no examples. Use when the task is well-understood by the model.

```text
You are a compliance officer for the NexTruzt.io escrow platform.

Analyze the following transaction and determine if it requires enhanced due diligence (EDD).

Transaction:
{transaction_json}

Respond with JSON: {"requires_edd": boolean, "risk_factors": string[], "recommendation": string}
```

**When to use:** Simple classification, formatting, translation, summarization.
**When to avoid:** Domain-specific terminology, unusual output formats, nuanced reasoning.

## Few-Shot Pattern

Include 2-5 diverse examples to demonstrate expected behavior.

```text
Classify the escrow dispute resolution outcome.

Example 1:
Dispute: "Buyer claims item was counterfeit. Seller provided no proof of authenticity."
Outcome: {"resolution": "buyer_refund", "confidence": 0.95, "reasoning": "No authenticity proof"}

Example 2:
Dispute: "Delivery was 2 days late but item was as described."
Outcome: {"resolution": "partial_refund", "confidence": 0.80, "reasoning": "Minor delay, item correct"}

Example 3:
Dispute: "Buyer changed mind after receiving item in perfect condition."
Outcome: {"resolution": "seller_payout", "confidence": 0.90, "reasoning": "Buyer's remorse, item as described"}

Now classify:
Dispute: "{dispute_text}"
Outcome:
```

**Example selection strategy:**
- Include at least one example per output category
- Order from simple → complex
- Include one edge case that demonstrates boundary behavior
- Use realistic data from the escrow domain (never real PII)

## Chain-of-Thought (CoT) Pattern

Force step-by-step reasoning before the final answer.

```text
Evaluate whether this escrow transaction should be flagged for AML review.

Think step by step:
1. Check transaction amount against reporting thresholds ($10,000 CTR, $3,000 structuring)
2. Evaluate sender/receiver risk profiles (country, history, verification status)
3. Check for structuring patterns (multiple transactions just below threshold)
4. Assess velocity (unusual frequency for this account)
5. Provide final determination with confidence score

Transaction:
{transaction_json}

Account History:
{history_json}

Step-by-step analysis:
```

**CoT variants:**
- **Zero-shot CoT:** Append "Let's think step by step" to any prompt
- **Manual CoT:** Provide explicit reasoning steps in the prompt
- **Auto CoT:** Let the model generate steps, then validate

## ReAct Pattern (Reason + Act)

For tool-using agents that must observe, think, then act.

```text
You are an escrow investigation agent with access to these tools:
- get_transaction(id): Returns transaction details
- get_account_history(account_id, days): Returns recent transactions
- check_sanctions(name, country): Returns sanctions screening result
- flag_for_review(transaction_id, reason): Flags a transaction

Investigate transaction {transaction_id} for potential fraud.

Use this format:
Thought: [your reasoning about what to do next]
Action: [tool_name(parameters)]
Observation: [tool result]
... repeat until investigation complete ...
Final Answer: [summary with recommendation]
```

## Self-Consistency Pattern

Run the same prompt N times and aggregate for high-confidence decisions.

```text
# Run 5 times with temperature=0.7, then majority-vote the outcome

Prompt (each run):
Given this escrow dispute, determine the fair resolution.
{dispute_details}

Options: A) Full refund to buyer  B) Full payout to seller  C) Split 50/50  D) Escalate to human

Aggregation:
- If 4/5+ agree → High confidence, use that answer
- If 3/5 agree → Medium confidence, flag for review
- If no majority → Low confidence, escalate to human mediator
```

## Prompt Chaining Pattern

Break complex tasks into sequential prompts where each output feeds the next.

```
Chain: Escrow Risk Assessment Pipeline

Prompt 1 (Extract) → "Extract all parties, amounts, and dates from this contract"
  ↓ output
Prompt 2 (Classify) → "Classify risk level based on these extracted fields: {prompt1_output}"
  ↓ output
Prompt 3 (Recommend) → "Given risk level {prompt2_output}, recommend escrow terms and conditions"
```

**Chain design rules:**
- Each prompt has exactly one job
- Pass structured data (JSON) between prompts
- Validate output format at each step before continuing
- Total chain token cost = sum of all prompts (plan accordingly)
