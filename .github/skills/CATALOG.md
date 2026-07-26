# Skills Catalog — NexTruzt.io

> 41 cross-platform skills organized by category. Each skill follows the Jeffallan `references/` pattern for memory-optimized lazy loading.
> **Version:** 2.1.0 | **Platforms:** Copilot CLI, Claude, Gemini

---

## Quick Reference

| # | Category | Skills | Description |
|---|----------|--------|-------------|
| 1 | [code-quality](#1-code-quality) | 7 | Code review, refactoring, documentation, debugging, quality metrics, smart refactor, tech debt |
| 2 | [security](#2-security) | 5 | OWASP audit, secret scanning, threat modeling, authentication, authorization |
| 3 | [architecture](#3-architecture) | 5 | Architecture review, design patterns, dependencies, legacy modernization, polyglot analysis |
| 4 | [testing](#4-testing) | 3 | Test generation, TDD coaching, coverage analysis |
| 5 | [database](#5-database) | 2 | Schema review, query optimization |
| 6 | [devops](#6-devops) | 4 | CI/CD, deployment preflight, monitoring, chaos engineering |
| 7 | [documentation](#7-documentation) | 3 | README, ADR, API docs |
| 8 | [research](#8-research) | 4 | Codebase exploration, tech spikes, spec mining, deep context generation |
| 9 | [project-management](#9-project-management) | 3 | Spec writing, issue creation, feature requirements |
| 10 | [ai](#10-ai) | 3 | MCP development, prompt engineering, agent orchestration |
| 11 | [language](#11-language) | 2 | .NET Core expert, C# developer |

---

## All Skills

### 1. Code Quality

Skills for reviewing, refactoring, documenting, and debugging code.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **code-reviewer** | Review code changes for correctness, style, security, and maintainability | review code, check PR, code review | [SKILL.md](./code-quality/code-reviewer/SKILL.md) |
| **refactor-planner** | Analyze code and produce a prioritized refactoring plan | refactor, clean up code, reduce tech debt | [SKILL.md](./code-quality/refactor-planner/SKILL.md) |
| **code-documenter** | Generate inline documentation, XML doc comments, and usage examples | document code, add comments, explain code | [SKILL.md](./code-quality/code-documenter/SKILL.md) |
| **debugging-wizard** | Systematic debugging with root cause analysis and fix verification | debug, error, stack trace, exception, crash | [SKILL.md](./code-quality/debugging-wizard/SKILL.md) |
| **quality-analyzer** | Analyze code quality metrics — cyclomatic complexity, cognitive complexity, maintainability index, SATD | analyze quality, code metrics, complexity analysis, maintainability | [SKILL.md](./code-quality/quality-analyzer/SKILL.md) |
| **smart-refactor** | Metrics-driven refactoring with baseline/after comparison and Fowler patterns | smart refactor, measure refactor, complexity reduction | [SKILL.md](./code-quality/smart-refactor/SKILL.md) |
| **tech-debt-tracker** | Detect, quantify, and prioritize technical debt — SATD detection, hour estimation, sprint planning | track tech debt, SATD scan, debt inventory, debt report | [SKILL.md](./code-quality/tech-debt-tracker/SKILL.md) |

---

### 2. Security

Skills for auditing, scanning, and modeling security threats.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **owasp-audit** | Audit code against OWASP Top 10 vulnerabilities | security audit, owasp check, vulnerability scan | [SKILL.md](./security/owasp-audit/SKILL.md) |
| **secret-scanner** | Detect hardcoded secrets, API keys, and credentials in source code | scan secrets, find credentials, check for keys | [SKILL.md](./security/secret-scanner/SKILL.md) |
| **threat-modeler** | Create STRIDE-based threat models for system components | threat model, security design, risk analysis | [SKILL.md](./security/threat-modeler/SKILL.md) |
| **authentication** | Implement authentication with Entra ID, IdentityServer, or ASP.NET Core Identity | authentication, login, Entra ID, JWT, OIDC | [SKILL.md](./security/authentication/SKILL.md) |
| **authorization** | Implement policy-based, role-based, and resource-based authorization | authorization, policies, roles, claims, access control | [SKILL.md](./security/authorization/SKILL.md) |

---

### 3. Architecture

Skills for reviewing architecture, recommending patterns, analyzing dependencies, and modernizing legacy systems.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **architecture-reviewer** | Review system architecture for quality attributes and anti-patterns | review architecture, check design, architecture audit | [SKILL.md](./architecture/architecture-reviewer/SKILL.md) |
| **design-pattern-advisor** | Recommend and apply appropriate design patterns to solve structural problems | suggest pattern, which pattern, design advice | [SKILL.md](./architecture/design-pattern-advisor/SKILL.md) |
| **dependency-analyzer** | Analyze project dependencies for risks, updates, and license compliance | check dependencies, audit packages, outdated packages | [SKILL.md](./architecture/dependency-analyzer/SKILL.md) |
| **legacy-modernizer** | Plan and execute modernization of legacy codebases to modern architectures | modernize, migrate, upgrade legacy, rewrite | [SKILL.md](./architecture/legacy-modernizer/SKILL.md) |
| **polyglot-analyzer** | Analyze multi-language codebases — language distribution, cross-language quality comparison, unified gates | polyglot analysis, multi-language, language distribution | [SKILL.md](./architecture/polyglot-analyzer/SKILL.md) |

---

### 4. Testing

Skills for generating tests, coaching TDD, and analyzing coverage.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **test-generator** | Generate unit and integration tests with Arrange-Act-Assert structure | write tests, generate tests, add test coverage | [SKILL.md](./testing/test-generator/SKILL.md) |
| **tdd-coach** | Guide test-driven development with red-green-refactor cycle | tdd, test first, red green refactor | [SKILL.md](./testing/tdd-coach/SKILL.md) |
| **test-coverage-analyzer** | Analyze test coverage gaps and recommend high-value tests to add | coverage gaps, missing tests, improve coverage | [SKILL.md](./testing/test-coverage-analyzer/SKILL.md) |

---

### 5. Database

Skills for reviewing schemas and optimizing queries.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **schema-reviewer** | Review database schema design for normalization, indexing, and integrity | review schema, check database design, schema audit | [SKILL.md](./database/schema-reviewer/SKILL.md) |
| **query-optimizer** | Analyze and optimize SQL queries for performance | optimize query, slow query, query performance | [SKILL.md](./database/query-optimizer/SKILL.md) |

---

### 6. DevOps

Skills for building CI/CD pipelines, validating deployments, monitoring, and chaos engineering.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **ci-cd-builder** | Create or improve CI/CD pipeline configurations | build pipeline, create CI, setup CD, github actions | [SKILL.md](./devops/ci-cd-builder/SKILL.md) |
| **deployment-preflight** | Run pre-deployment checks and generate go/no-go reports | preflight check, ready to deploy, deployment review | [SKILL.md](./devops/deployment-preflight/SKILL.md) |
| **monitoring-expert** | Design observability stacks with metrics, logs, traces, and alerting | monitoring, observability, alerts, dashboards, SLO | [SKILL.md](./devops/monitoring-expert/SKILL.md) |
| **chaos-engineer** | Design and execute chaos experiments to verify system resilience | chaos testing, resilience, fault injection, game day | [SKILL.md](./devops/chaos-engineer/SKILL.md) |

---

### 7. Documentation

Skills for generating READMEs, ADRs, and API documentation.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **readme-generator** | Generate comprehensive README files from project analysis | create readme, write readme, project documentation | [SKILL.md](./documentation/readme-generator/SKILL.md) |
| **adr-creator** | Create Architecture Decision Records following the ADR standard | create ADR, document decision, architecture decision | [SKILL.md](./documentation/adr-creator/SKILL.md) |
| **api-documenter** | Generate API documentation from code with examples and schemas | document API, API docs, endpoint documentation | [SKILL.md](./documentation/api-documenter/SKILL.md) |

---

### 8. Research

Skills for exploring codebases, planning technical spikes, and mining specifications.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **codebase-explorer** | Explore and map unfamiliar codebases to build understanding | explore codebase, understand code, map architecture | [SKILL.md](./research/codebase-explorer/SKILL.md) |
| **tech-spike-planner** | Plan time-boxed technical investigations with clear success criteria | plan spike, technical investigation, research task | [SKILL.md](./research/tech-spike-planner/SKILL.md) |
| **spec-miner** | Extract implicit specifications from code, tests, and documentation | mine specs, extract requirements, reverse engineer | [SKILL.md](./research/spec-miner/SKILL.md) |
| **deep-context-generator** | Generate LLM-optimized codebase context for onboarding and architecture understanding | generate context, codebase overview, onboarding, architecture map | [SKILL.md](./research/deep-context-generator/SKILL.md) |

---

### 9. Project Management

Skills for writing specifications, creating issues, and forging feature requirements.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **spec-writer** | Write comprehensive technical specifications from feature requests | write spec, create specification, define requirements | [SKILL.md](./project-management/spec-writer/SKILL.md) |
| **issue-creator** | Create structured GitHub issues with acceptance criteria and sub-task decomposition | create issue, write issue, file bug, create ticket | [SKILL.md](./project-management/issue-creator/SKILL.md) |
| **feature-forge** | Generate complete feature breakdowns with stories, tasks, and acceptance criteria | feature breakdown, user stories, requirements, epic | [SKILL.md](./project-management/feature-forge/SKILL.md) |

---

### 10. AI

Skills for MCP development, prompt engineering, and multi-agent orchestration.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **mcp-developer** | Build, debug, and extend MCP servers/clients — tool handlers, resources, transports, schemas | MCP, Model Context Protocol, MCP server, AI tools, JSON-RPC | [SKILL.md](./ai/mcp-developer/SKILL.md) |
| **prompt-engineer** | Write, refactor, and evaluate LLM prompts — templates, structured outputs, evaluation rubrics | prompt engineering, prompt optimization, chain-of-thought, few-shot, system prompts | [SKILL.md](./ai/prompt-engineer/SKILL.md) |
| **agent-orchestrator** | Orchestrate parallel sub-agent fleets with token-aware delegation, DAG dependencies, and result aggregation | orchestrate agents, parallel tasks, multi-agent, fleet management, token budget | [SKILL.md](./ai/agent-orchestrator/SKILL.md) |

---

### 11. Language

Language-specific skills for .NET Core and C# development.

| Skill | Description | Triggers | Link |
|-------|-------------|----------|------|
| **dotnet-core-expert** | Deep .NET 10 expertise — minimal APIs, Clean Architecture, EF Core, CQRS/MediatR, JWT auth, AOT | .NET Core, .NET 10, ASP.NET Core, C# 13, minimal API, Entity Framework Core, microservices | [SKILL.md](./language/dotnet-core-expert/SKILL.md) |
| **csharp-developer** | Senior C# 13 developer — records, pattern matching, primary constructors, Blazor, performance | C#, .NET, Blazor, Entity Framework, EF Core, SignalR, Minimal API | [SKILL.md](./language/csharp-developer/SKILL.md) |

---

## Directory Structure

```
.github/
└── skills/
    ├── CATALOG.md                                  # ← This file (master index)
    ├── code-quality/
    │   ├── code-reviewer/SKILL.md + references/
    │   ├── refactor-planner/SKILL.md + references/
    │   ├── code-documenter/SKILL.md + references/
    │   ├── debugging-wizard/SKILL.md + references/
    │   ├── quality-analyzer/SKILL.md + references/
    │   ├── smart-refactor/SKILL.md + references/
    │   └── tech-debt-tracker/SKILL.md + references/
    ├── security/
    │   ├── owasp-audit/SKILL.md + references/
    │   ├── secret-scanner/SKILL.md + references/
    │   ├── threat-modeler/SKILL.md + references/
    │   ├── authentication/SKILL.md + references/
    │   └── authorization/SKILL.md + references/
    ├── architecture/
    │   ├── architecture-reviewer/SKILL.md + references/
    │   ├── design-pattern-advisor/SKILL.md + references/
    │   ├── dependency-analyzer/SKILL.md + references/
    │   ├── legacy-modernizer/SKILL.md + references/
    │   └── polyglot-analyzer/SKILL.md + references/
    ├── testing/
    │   ├── test-generator/SKILL.md + references/
    │   ├── tdd-coach/SKILL.md + references/
    │   └── test-coverage-analyzer/SKILL.md + references/
    ├── database/
    │   ├── schema-reviewer/SKILL.md + references/
    │   └── query-optimizer/SKILL.md + references/
    ├── devops/
    │   ├── ci-cd-builder/SKILL.md + references/
    │   ├── deployment-preflight/SKILL.md + references/
    │   ├── monitoring-expert/SKILL.md + references/
    │   └── chaos-engineer/SKILL.md + references/
    ├── documentation/
    │   ├── readme-generator/SKILL.md + references/
    │   ├── adr-creator/SKILL.md + references/
    │   └── api-documenter/SKILL.md + references/
    ├── research/
    │   ├── codebase-explorer/SKILL.md + references/
    │   ├── tech-spike-planner/SKILL.md + references/
    │   ├── spec-miner/SKILL.md + references/
    │   └── deep-context-generator/SKILL.md + references/
    ├── project-management/
    │   ├── spec-writer/SKILL.md + references/
    │   ├── issue-creator/SKILL.md + references/
    │   └── feature-forge/SKILL.md + references/
    ├── ai/
    │   ├── mcp-developer/SKILL.md + references/
    │   ├── prompt-engineer/SKILL.md + references/
    │   └── agent-orchestrator/SKILL.md + references/
    └── language/
        ├── dotnet-core-expert/SKILL.md + references/
        └── csharp-developer/SKILL.md + references/
```

## Using Skills

### Copilot CLI

```bash
# Skills in .github/skills/ are automatically discovered
# Trigger by name or keyword:
copilot "Use the agent-orchestrator skill to coordinate parallel analysis"
copilot "Use the csharp-developer skill to write a Blazor component"
```

### Claude

1. **Project Knowledge** — Add `SKILL.md` files to your Claude project's knowledge base
2. **Direct Reference** — Ask Claude: *"Follow the prompt-engineer skill to design a prompt for [task]"*

### Gemini

1. **Context Window** — Paste the `SKILL.md` content at the start of your conversation
2. **Gems** — Create a custom Gem with the skill content as instructions

## Conventions

- Each skill lives in `<category>/<skill-name>/SKILL.md` with a `references/` directory
- Reference files are lazy-loaded — only read when their "Load When" condition is met
- All skills target v2.0.0 with `allowed-tools`, `related-skills`, and `output-format` metadata
- Skills are self-contained but declare related skills for cross-referencing
- All skills target the same three platforms: `copilot-cli`, `claude`, `gemini`

---

*Catalog maintained by NexTruzt — MIT License*
