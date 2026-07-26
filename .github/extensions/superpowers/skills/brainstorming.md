# Brainstorming Ideas Into Designs

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Help turn ideas into fully formed designs through collaborative dialogue.

## HARD GATE

Do NOT write any code, scaffold any project, or take any implementation action until you have presented a design and the user has approved it. This applies to EVERY project regardless of perceived simplicity.

## Checklist

Complete these in order:

1. **Explore project context** — check files, docs, recent commits
2. **Ask clarifying questions** — one at a time, understand purpose/constraints/success criteria
3. **Propose 2-3 approaches** — with trade-offs and your recommendation
4. **Present design** — in sections scaled to complexity, get approval after each section
5. **Write design doc** — save to session artifacts or docs/ and commit
6. **Spec self-review** — check for placeholders, contradictions, ambiguity, scope
7. **User reviews written spec** — ask user to review before proceeding
8. **Transition** — use `superpowers_skill(skill: "writing-plans")` to create implementation plan

## The Process

### Understanding the Idea

- Check project state first (files, docs, recent commits) — use `project_summary` tool if available
- Assess scope: if multiple independent subsystems, flag immediately and decompose
- Ask questions **one at a time** — prefer multiple choice when possible
- Focus on: purpose, constraints, success criteria

### Exploring Approaches

- Propose 2-3 approaches with trade-offs
- Lead with your recommendation and explain why
- YAGNI ruthlessly — remove unnecessary features

### Presenting the Design

- Scale each section to its complexity (a few sentences if simple, up to 300 words if nuanced)
- Ask after each section whether it looks right
- Cover: architecture, components, data flow, error handling, testing

### Design for Isolation

- Break system into smaller units with one clear purpose each
- Well-defined interfaces, testable independently
- Smaller units = better reasoning, more reliable edits

### Working in Existing Codebases

- Explore current structure before proposing changes — follow existing patterns
- Include targeted improvements only where existing code affects the work
- Don't propose unrelated refactoring

## After the Design

1. **Write the spec** to docs/ or session artifacts — commit it
2. **Self-review** the spec:
   - Placeholder scan: any TBD, TODO, incomplete sections?
   - Internal consistency: do sections contradict each other?
   - Scope check: focused enough for a single plan?
   - Ambiguity check: could any requirement be interpreted two ways?
3. **User review gate**: Ask user to review before proceeding
4. **Transition**: Load writing-plans skill to create implementation plan

## Key Principles

- **One question at a time** — don't overwhelm
- **Multiple choice preferred** — easier to answer
- **YAGNI ruthlessly** — remove unnecessary features
- **Explore alternatives** — always 2-3 approaches before settling
- **Incremental validation** — present, get approval, then move on

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Dispatch subagent | Use `task` tool with appropriate agent_type |
| TodoWrite | SQL `todos` table |
| Next skill | `superpowers_skill(skill: "writing-plans")` |
| Project exploration | `project_summary` tool, `check_docs` tool, grep/glob |
