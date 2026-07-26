# Decision Drivers Reference

Guidance for identifying and prioritizing criteria that drive architectural decisions in the NexTruzt.io escrow platform.

## What Are Decision Drivers?

Decision drivers are the specific, measurable criteria used to evaluate ADR options. They transform subjective debates into structured comparisons.

❌ Vague: "Good performance"
✅ Specific: "Sub-200ms P99 API response time under 500 concurrent users"

## Driver Categories

### Business Drivers

- **Revenue impact** — Effect on transaction throughput or onboarding speed
- **Time-to-market** — Delivery speed to production
- **Operational cost** — Licensing, hosting, and maintenance costs
- **Regulatory compliance** — PCI-DSS, SOC 2, AML/KYC, GDPR requirements

### Technical Drivers

- **Performance** — Latency, throughput, resource utilization under load
- **Security** — Attack surface, encryption, access control
- **Data integrity** — ACID guarantees, consistency models, backup/restore
- **Maintainability** — Code complexity, testability, onboarding friction
- **Observability** — Logging, metrics, tracing capabilities

### Organizational Drivers

- **Team expertise** — Production experience with the technology
- **Vendor risk** — Stability of vendor or community backing
- **Migration effort** — Cost of transitioning from current approach
- **Alignment** — Fit with organization's broader technology strategy

## Fintech-Specific Quality Attributes

| Attribute | Key Questions |
|---|---|
| **Auditability** | Can all state changes be traced to an actor/timestamp? Append-only logging? Tamper-evident? |
| **Transaction Integrity** | ACID for fund movements? Handles partial failures? Idempotent retries? |
| **Compliance** | PCI-DSS, SOC 2 Type II, AML/KYC, GDPR, data residency? |
| **Security** | Encryption at rest/transit? Least privilege? Key Vault for secrets? |

## Stakeholder Concern Mapping

| Stakeholder | Primary Concerns |
|---|---|
| Product Owner | Time-to-market, feature completeness |
| Security Team | Threat surface, compliance, data protection |
| DevOps / SRE | Operability, observability, deployment complexity |
| Developers | Maintainability, testability, DX |
| Compliance Officer | Regulatory requirements, audit capabilities |

## Prioritization

- **Must Have** — Non-negotiable; options that fail are eliminated
- **Should Have** — Important but can be compromised with justification
- **Nice to Have** — Desirable but not a deciding factor

## Making Drivers Measurable

| Qualitative | Measurable |
|---|---|
| "Fast" | P99 latency < 200ms at 500 concurrent users |
| "Secure" | Zero critical CVEs; passes OWASP Top 10 scan |
| "Scalable" | Handles 10x current volume without architecture change |
| "Cost-effective" | TCO < $X/month at projected scale |

## Decision Matrix Technique

```markdown
| Driver (Weight) | Option A | Option B | Option C |
|---|---|---|---|
| Transaction integrity (5) | ✅ Strong (5) | ⚠️ Moderate (3) | ✅ Strong (5) |
| Team expertise (4) | ✅ High (4) | ✅ High (4) | ❌ Low (1) |
| Compliance (5) | ✅ Built-in (5) | ⚠️ Manual (2) | ✅ Built-in (5) |
| Operational cost (3) | ⚠️ Medium (3) | ✅ Low (5) | ❌ High (1) |
| **Weighted Total** | **72** | **55** | **52** |
```

## Example Drivers for Common .NET Decisions

**ORM (EF Core vs Dapper):** Query performance, developer productivity, migration tooling, LINQ support, raw SQL escape hatch.

**Auth (Entra ID vs Custom Identity):** Enterprise SSO compliance, MFA/conditional access, Azure integration, vendor lock-in.

**Messaging (MediatR vs Service Bus):** Latency requirements, cross-service decoupling, message durability, operational complexity.
