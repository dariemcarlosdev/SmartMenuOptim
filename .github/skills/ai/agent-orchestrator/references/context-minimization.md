# Context Minimization Reference

> **Load when:** Writing agent prompts, reducing token waste, applying the Shared Context Protocol.

## Shared Context Protocol

The Shared Context Protocol eliminates context duplication across agent fleets by splitting prompts into three layers:

```
┌─────────────────────────────────────────────────┐
│ Agent Prompt                                     │
│                                                  │
│ ┌─────────────────────────────────────────────┐  │
│ │ Common Preamble (~2K tokens)                │  │
│ │ - Project identity & architecture           │  │
│ │ - Coding conventions & constraints          │  │
│ │ - Tech stack summary                        │  │
│ │ (shared across ALL agents — written once)    │  │
│ ├─────────────────────────────────────────────┤  │
│ │ Task-Specific Delta (~1-5K tokens)          │  │
│ │ - Exact files/functions to analyze or modify │  │
│ │ - Specific requirements for THIS unit        │  │
│ │ - Expected output for THIS unit             │  │
│ │ (unique per agent — minimal overlap)         │  │
│ ├─────────────────────────────────────────────┤  │
│ │ Output Format (~500 tokens)                 │  │
│ │ - Response structure                        │  │
│ │ - Quality criteria                          │  │
│ │ (shared template — customized per agent)     │  │
│ └─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

### Common Preamble Template

```text
## Project Context
NexTruzt.io is a fintech escrow platform built with .NET 10, Blazor Server, Clean Architecture, CQRS/MediatR. 

## Architecture
- Domain: Entities, value objects, aggregates (zero external dependencies)
- Application: Commands, queries, handlers, validators (MediatR + FluentValidation)
- Infrastructure: EF Core, external services, repository implementations
- Presentation: Minimal APIs, Blazor Server components

## Conventions
- File-scoped namespaces, nullable enabled, sealed classes by default
- Code-behind for Blazor (.razor + .razor.cs), scoped CSS per component
- CancellationToken on all async methods, IOptions<T> for configuration
- Arrange-Act-Assert testing with descriptive method names
```

### Task-Specific Delta Examples

```text
# Delta for explore-agent analyzing auth module:
Analyze the authentication module in src/Infrastructure/Identity/.
Focus on: JWT configuration, policy definitions, claims transformation.
Report: interfaces exposed, authorization policies, token configuration.
Files: src/Infrastructure/Identity/**, src/Application/Common/Auth/**

# Delta for general-purpose agent implementing a handler:
Implement CreateEscrowHandler in src/Application/Features/Escrows/CreateEscrow/.
The command record already exists at CreateEscrowCommand.cs.
Use IEscrowRepository (interface at src/Application/Interfaces/IEscrowRepository.cs).
Follow the pattern in src/Application/Features/Payments/CreatePayment/CreatePaymentHandler.cs.
```

## Context Deduplication Rules

### Rule 1: File Paths Over File Contents

```text
# BAD — 500 tokens for inline code
Here is the EscrowRepository implementation:
```csharp
public sealed class EscrowRepository : IEscrowRepository
{
    private readonly EscrowDbContext _context;
    // ... 30 lines ...
}
```

# GOOD — 30 tokens for reference
File: src/Infrastructure/Repositories/EscrowRepository.cs
The agent has file access — it can read this directly.
```

### Rule 2: Function Signatures Over Full Classes

```text
# BAD — entire class (~200 tokens)
Include the full IEscrowRepository interface with all methods...

# GOOD — relevant signature only (~40 tokens)
Interface IEscrowRepository has: Task<Escrow?> FindByIdAsync(EscrowId id, CancellationToken ct)
```

### Rule 3: Pattern References Over Repeated Instructions

```text
# BAD — repeating conventions in every agent prompt (~300 tokens each)
"Use file-scoped namespaces. Make the class sealed. Use primary constructors.
 Add CancellationToken. Use records for DTOs..."

# GOOD — reference the common preamble (~20 tokens)
Follow project conventions from the Common Preamble above.
```

### Rule 4: Diff Over Full State

```text
# BAD — full file after changes (~500 tokens)
"Here's the complete updated EscrowService.cs: ..."

# GOOD — describe the change (~50 tokens)
In EscrowService.cs, add a new method:
  Task<Result> ReleaseAsync(EscrowId id, CancellationToken ct)
  that calls repository.UpdateStatusAsync(id, EscrowStatus.Released, ct)
```

## Progressive Context Disclosure

Load context in stages — don't dump everything upfront.

```
Stage 1 (initial prompt): Project overview + task description (~3K tokens)
  Agent works on initial analysis...

Stage 2 (follow-up if needed): Specific file contents the agent requests (~2K tokens)
  Agent refines its approach...

Stage 3 (follow-up if needed): Edge cases and constraints (~1K tokens)
  Agent completes the task...
```

**When to use:** Complex tasks where the agent may not need all context. Start minimal, add on demand.

## Token Estimation Guide

| Content Type | Approximate Tokens | Optimization |
|---|---|---|
| File path reference | 10-30 | Always prefer over inline content |
| Function signature | 20-50 | Prefer over full class listing |
| Full C# class (~50 lines) | 300-500 | Only include if agent must analyze internals |
| Preamble (project context) | 500-2,000 | Write once, include in all agents |
| Task-specific delta | 500-3,000 | Keep as small as possible |
| Few-shot example | 100-300 each | Maximum 3 per agent |
| Full file (~200 lines) | 1,000-2,000 | Rarely needed — use path reference |

## Anti-Patterns

| Anti-Pattern | Token Waste | Fix |
|---|---|---|
| Including full project README in every agent | ~2K × N agents | Extract relevant sections only |
| Pasting entire files when 1 method is relevant | ~1K per file | Use function signature or line range |
| Repeating coding conventions in each delta | ~300 × N agents | Put in shared preamble |
| Including examples the agent won't use | ~200 per example | Only include examples for the specific task |
| Specifying default behavior the model already follows | ~100 per instruction | Remove instructions for model defaults |

## Context Budget Worksheet

```markdown
## Agent: {agent_id}
| Section | Tokens | Notes |
|---|---|---|
| Common Preamble | {n} | Shared across fleet |
| Task-Specific Delta | {n} | Unique to this agent |
| Output Format | {n} | Shared template |
| **Total Prompt** | **{sum}** | Budget: {max} |
| Expected Output | {n} | Agent's response |
| **Total Context** | **{sum}** | Must be < context window × 0.8 |
```
