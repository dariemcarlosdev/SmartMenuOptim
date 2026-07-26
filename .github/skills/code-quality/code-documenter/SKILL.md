---
name: code-documenter
description: "Generate XML doc comments, JSDoc/TSDoc, inline comments, and README sections for code — triggered by 'document code', 'add docs', 'generate documentation'"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: code-quality
  triggers: document code, add docs, generate documentation, add comments, document API, document module, missing docs, undocumented
  role: specialist
  scope: documentation
  platforms: copilot-cli, claude, gemini
  output-format: document
  related-skills: code-reviewer, refactor-planner, owasp-audit
---

# Code Documenter

A documentation generation skill that analyzes code and produces XML doc comments (C#), JSDoc/TSDoc (JS/TS), clarifying inline comments, and README sections — prioritizing self-documenting code over excessive commenting.

## When to Use This Skill

- "Document this code" or "Add docs to this file"
- "Generate XML doc comments for public API"
- "This module needs a README"
- "What's missing documentation?"
- Before publishing a library or NuGet package
- During documentation sprints or compliance audits

## Core Workflow

1. **Analyze Code Structure** — Identify language/framework to determine comment format (C# → XML doc, TS → JSDoc/TSDoc). Map the public API surface: classes, interfaces, methods, properties, enums, endpoints.
   - **Checkpoint:** API surface mapped, documentation format confirmed.

2. **Identify Gaps** — Scan for undocumented public members. Prioritize: P0 (public APIs, interfaces), P1 (classes, constructors), P2 (properties, enums), P3 (complex private methods). Load `references/xml-documentation.md` for C# or `references/jsdoc-tsdoc.md` for JS/TS.
   - **Checkpoint:** Gap list complete with priority assignments.

3. **Generate Documentation** — Apply language-appropriate format. Include `<summary>`, `<param>`, `<returns>`, `<exception>` tags. Add `<see cref=""/>` cross-references. Add usage `<example>` blocks for complex APIs.
   - **Checkpoint:** All P0/P1 members documented before moving to inline comments.

4. **Add Inline Comments** — Comment only non-obvious logic: business rules, workarounds, perf optimizations, regex patterns. Load `references/comment-anti-patterns.md` for what NOT to comment.
   - **Checkpoint:** Verify no "stating the obvious" comments added.

5. **Generate Module README** — If documenting a module/component, produce a README section. Load `references/readme-standards.md` for structure template.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| XML Documentation | `references/xml-documentation.md` | C# XML docs |
| JSDoc / TSDoc | `references/jsdoc-tsdoc.md` | JavaScript/TypeScript docs |
| README Standards | `references/readme-standards.md` | Writing READMEs |
| Comment Anti-Patterns | `references/comment-anti-patterns.md` | Reviewing comment quality |

## Quick Reference

```csharp
/// <summary>
/// Releases escrowed funds to the seller after buyer confirmation.
/// </summary>
/// <param name="command">Release command with escrow ID and authorization.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>Result indicating success or validation/authorization failure.</returns>
/// <exception cref="EscrowNotFoundException">Escrow ID does not exist.</exception>
public async Task<Result> ReleaseAsync(
    ReleaseEscrowCommand command,
    CancellationToken cancellationToken = default)
```

```csharp
// DO: Explain WHY (business rule not obvious from code)
// Escrow funds are held for 24h after buyer confirmation per regulatory requirement
await Task.Delay(TimeSpan.FromHours(24), ct);

// DON'T: State the obvious
var order = await repo.GetByIdAsync(id); // Get order by ID  ← NOISE
```

## Constraints

### MUST DO
- Document ALL public members — no gaps in public API documentation
- Use language-appropriate format (XML doc, JSDoc, etc.)
- Include `<param>`, `<returns>`, and `<exception>` for all public methods
- Add usage examples for non-trivial public APIs
- Keep documentation concise — one summary sentence, then details only if needed
- Use `<see cref="..."/>` / `{@link ...}` for cross-references

### MUST NOT
- Do not state the obvious — `/// Gets or sets the name` on `Name` is noise
- Do not document private members unless genuinely complex
- Do not write documentation longer than the code it describes
- Do not document implementation details that may change
- Do not add inline comments to simple, readable code
- Do not leave TODO placeholders in documentation

## Output Template

```markdown
# Documentation Report

**Scope:** [Files/module]  |  **Date:** YYYY-MM-DD

## Documentation Coverage
| Category | Total | Documented | Gap |
|----------|-------|-----------|-----|

## Generated Documentation
### File: [path]
[XML doc comments / JSDoc ready to apply]

## Inline Comments Added
| File | Line | Comment | Reason |

## Module README (if applicable)
```
