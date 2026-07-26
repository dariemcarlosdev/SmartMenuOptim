# Specification Template

Structured template for technical specifications.

## Full Specification Document

```markdown
# Technical Specification: {Feature/Change Title}

**Author:** {Name}
**Date:** {YYYY-MM-DD}
**Status:** Draft | In Review | Approved
**Version:** 1.0

---

## 1. Problem Statement

{What pain or gap exists today? Why does this matter?
Include metrics if available: error rates, user complaints, revenue impact.}

## 2. Goals

- **Goal 1:** {Measurable outcome with success metric}
- **Goal 2:** {Measurable outcome with success metric}
- **Non-Goals:** {What this spec explicitly does NOT address}

## 3. Assumptions

- {Assumption 1 — flagged for validation}
- {Assumption 2 — flagged for validation}

## 4. Scope

### In Scope
- {Deliverable or area of work}

### Out of Scope
- {Explicitly excluded item}

## 5. Functional Requirements

| ID     | Requirement                | Priority | Acceptance Criteria          |
|--------|----------------------------|----------|------------------------------|
| FR-001 | {User story or capability} | High     | {Testable success condition} |
| FR-002 | {User story or capability} | Medium   | {Testable success condition} |

## 6. Non-Functional Requirements

| ID      | Category    | Requirement               | Target             |
|---------|-------------|---------------------------|---------------------|
| NFR-001 | Performance | {Description}             | {Measurable target} |
| NFR-002 | Security    | {Description}             | {Measurable target} |
| NFR-003 | Reliability | {Description}             | {SLA target}        |

## 7. Technical Design

### 7.1 Architecture Overview
{High-level approach and affected Clean Architecture layers}

### 7.2 Data Model
{New or modified entities, EF Core configurations, migrations}

### 7.3 API / Interface Changes
{MediatR commands/queries, DTOs, endpoint contracts}

### 7.4 Flow Description
{Step-by-step flow: HTTP → Controller → MediatR → Handler → Repository}

## 8. Dependencies and Risks

### Dependencies
| Dependency              | Owner       | Status   |
|-------------------------|-------------|----------|
| {External system/team}  | {Owner}     | {Status} |

### Risks
| Risk                    | Likelihood | Impact | Mitigation            |
|-------------------------|------------|--------|-----------------------|
| {Risk description}      | H/M/L      | H/M/L  | {Strategy}            |

## 9. Testing Strategy

| Test Type       | Scope              | Criteria                |
|-----------------|--------------------|-------------------------|
| Unit            | {Handlers, domain} | {Coverage target}       |
| Integration     | {API endpoints}    | {Scenarios}             |
| E2E             | {User workflows}   | {Pass/fail}             |

## 10. Open Questions

- [ ] {Question needing stakeholder input}
- [ ] {Question needing investigation}
```

## .NET-Specific Sections

### Data Model Section Example

```csharp
// New entity
public sealed class EscrowTransaction : BaseEntity
{
    public EscrowId EscrowId { get; private set; }
    public Money Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

// EF Core configuration
public sealed class EscrowTransactionConfiguration
    : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.HasIndex(e => e.EscrowId);
    }
}
```

### API Contract Section Example

```csharp
// MediatR Command
public sealed record CreateEscrowCommand(
    Guid BuyerId,
    Guid SellerId,
    decimal Amount,
    string Currency
) : IRequest<Result<EscrowResponse>>;

// Response DTO
public sealed record EscrowResponse(
    Guid Id,
    string Status,
    decimal Amount,
    DateTimeOffset CreatedAt);
```

## Spec Review Checklist

Before submitting for review, verify:

- [ ] Problem statement explains WHY, not just WHAT
- [ ] All requirements are numbered (FR-xxx, NFR-xxx)
- [ ] Every requirement has a testable acceptance criterion
- [ ] Out of scope is explicitly stated
- [ ] Assumptions are listed and flagged for validation
- [ ] Technical design covers all affected layers
- [ ] Risks have mitigations
- [ ] Open questions are captured for follow-up
