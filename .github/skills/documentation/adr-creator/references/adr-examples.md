# ADR Examples

Condensed ADR examples for the NexTruzt.io escrow platform (.NET 10, Clean Architecture).

---

## Example 1: Adopt CQRS with MediatR

```markdown
# ADR-0002: Adopt CQRS with MediatR over Traditional Repository Pattern
**Date:** 2024-02-01 | **Status:** Accepted

## Context and Problem Statement
Escrow writes (agreements, fund releases, disputes) require rich domain logic and audit trails.
Reads (dashboards, reports) need optimized queries. Traditional repositories force both through
the domain model, creating friction.

## Decision Drivers
- Writes require complex validation and audit logging
- Reads need query optimization without domain model overhead
- Cross-cutting concerns (logging, validation) must not be duplicated

## Considered Options
1. **CQRS with MediatR** — Separate command/query pipelines
2. **Traditional Repository** — Generic repos with service orchestration
3. **Vertical Slice** — Feature-organized handlers without CQRS split

## Decision Outcome
Chosen option: **"CQRS with MediatR"**, because it separates write-side domain
logic from read-side optimization; pipeline behaviors handle cross-cutting concerns.

### Consequences
- Good, because handlers encapsulate domain logic with clear boundaries
- Good, because reads can use Dapper/projections independently
- Bad, because more files per feature; higher onboarding friction
```

---

## Example 2: Select PostgreSQL for Escrow Ledger

```markdown
# ADR-0005: Select PostgreSQL over SQL Server for Escrow Ledger
**Date:** 2024-04-15 | **Status:** Accepted

## Context and Problem Statement
The escrow ledger records every fund movement. The database must prioritize ACID integrity,
auditability, and cost-effectiveness. Evaluating PostgreSQL vs SQL Server for JSONB support
and licensing costs across multiple environments.

## Decision Drivers
- ACID transactions for fund movements — partial transfers unacceptable
- Licensing cost across dev/staging/QA/prod environments
- JSONB for flexible escrow agreement metadata
- EF Core integration maturity (Npgsql)

## Considered Options
1. **PostgreSQL 16** — Open-source, Npgsql/EF Core provider
2. **SQL Server 2022** — Enterprise RDBMS, first-party EF Core
3. **CockroachDB** — Distributed SQL, PostgreSQL-compatible

## Decision Outcome
Chosen option: **"PostgreSQL 16"**, because it provides ACID transactions, native JSONB,
row-level security for multi-tenant isolation, and zero licensing costs.

### Consequences
- Good, because zero licensing saves ~$40K/year; JSONB eliminates document storage need
- Good, because `pg_audit` provides compliance-grade audit logging
- Bad, because DBA team needs PostgreSQL training; fewer GUI tools
```

---

## Example 3: Adopt Entra ID for Authentication

```markdown
# ADR-0008: Adopt Microsoft Entra ID over Custom ASP.NET Core Identity
**Date:** 2024-07-10 | **Status:** Accepted

## Context and Problem Statement
The platform needs auth for escrow agents, buyers, sellers, compliance officers, and admins.
Built on .NET 10 Blazor Server / Azure. Choosing between self-managed identity and managed
identity provider — critical for fintech security posture and compliance burden.

## Decision Drivers
- MFA enforcement for all user types (regulatory requirement)
- Enterprise SSO for B2B escrow partnerships
- SOC 2 audit logs for authentication events
- Minimize operational burden of identity infrastructure

## Considered Options
1. **Microsoft Entra ID** — Managed identity via `Microsoft.Identity.Web`
2. **ASP.NET Core Identity** — Self-hosted with local user store
3. **Duende IdentityServer** — Self-hosted OIDC provider

## Decision Outcome
Chosen option: **"Microsoft Entra ID"**, because it provides built-in MFA/conditional access,
audit logs satisfy SOC 2, and Managed Identity eliminates secrets for Azure services.

### Consequences
- Good, because MFA/conditional access built-in; SSO federation is configuration-only
- Good, because Managed Identity eliminates credential management
- Bad, because B2C requires Entra External ID (added cost); vendor lock-in
```

---

**Usage:** Match section order (Context → Drivers → Options → Outcome), be domain-specific,
balance pros/cons for all options, and link related ADRs.
