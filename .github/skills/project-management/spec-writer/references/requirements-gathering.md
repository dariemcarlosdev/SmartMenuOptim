# Requirements Gathering

Techniques for eliciting requirements through structured interviews.

## Interview Framework

### Stakeholder Identification

| Stakeholder Type | Questions Focus | Priority |
|-----------------|----------------|----------|
| Product Owner | Business value, priorities, success metrics | Critical |
| End User | Workflows, pain points, expectations | Critical |
| Developer | Technical constraints, existing patterns | High |
| QA/Tester | Edge cases, failure modes, testability | High |
| Security | Auth, data protection, compliance | High |
| Operations | Deployment, monitoring, SLA requirements | Medium |

### Opening Questions (Context Setting)

```
1. What problem are we solving? Who experiences this pain?
2. What happens today without this feature?
3. What does success look like? How will we measure it?
4. Who are the primary users? What are their technical skill levels?
5. What's the timeline? Are there hard deadlines (regulatory, contractual)?
```

### Functional Requirement Questions

```
1. Walk me through the ideal workflow step by step.
2. What data does the user need to provide? What data do they receive?
3. What happens when the user makes a mistake? (error paths)
4. Are there different user roles with different capabilities?
5. What existing features does this interact with?
6. What's the minimum viable version? What can be deferred?
```

### Non-Functional Requirement Questions

```
Performance:
- How many users will use this simultaneously?
- What response time is acceptable? What's unacceptable?
- How much data will this process (volume, growth rate)?

Security:
- What data is sensitive? PII, financial, health?
- Who should NOT have access to this feature?
- Are there compliance requirements (PCI-DSS, SOX, GDPR)?

Reliability:
- What happens if this feature is unavailable?
- What's the acceptable downtime per month?
- Does this need to work offline or in degraded mode?
```

## NexTruzt Escrow-Specific Questions

### Financial Transaction Features

```
1. What are the minimum and maximum transaction amounts?
2. What currencies are supported?
3. What are the escrow lifecycle states? (Created → Funded → Released → Closed)
4. Who can initiate/approve each state transition?
5. What happens to funds if a dispute is raised?
6. What audit trail is required for regulatory compliance?
7. What are the timeout/expiration rules?
```

### Authorization Questions

```
1. What roles exist? (Buyer, Seller, Agent, Admin, Auditor)
2. What can each role see vs. modify?
3. Is multi-party approval required for any action?
4. How is identity verified? (Entra ID, KYC)
5. What actions require elevated authorization?
```

## Requirement Elicitation Techniques

| Technique | Best For | When to Use |
|-----------|----------|-------------|
| **Interview** | Understanding context and motivation | Starting a new feature |
| **Observation** | Discovering actual vs. stated workflows | Improving existing features |
| **Prototyping** | Validating UI/UX assumptions | User-facing features |
| **Document Analysis** | Regulatory/compliance requirements | Financial/legal features |
| **User Story Mapping** | Prioritizing feature scope | Sprint planning |
| **Event Storming** | Complex domain workflows | DDD domain modeling |

## Requirements Validation Checklist

Before finalizing gathered requirements:

- [ ] Each requirement traces to a stakeholder need
- [ ] No two requirements contradict each other
- [ ] Every requirement is testable (has clear pass/fail)
- [ ] Assumptions are separated from confirmed requirements
- [ ] Priority is assigned (MoSCoW or High/Medium/Low)
- [ ] Open questions are captured with assigned owners
