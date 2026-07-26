# Interview Questions

Structured elicitation questions for requirements workshops.

## Workshop Opening (5 minutes)

```
1. What is the feature we're defining today?
2. Who requested this? What business goal does it serve?
3. Who are the end users? What are their skill levels?
4. What does success look like? How will we measure it?
5. Are there hard deadlines (regulatory, contractual, launch)?
```

## Functional Requirements (20 minutes)

### Workflow Discovery

```
1. Walk me through the ideal user workflow, step by step.
2. What triggers this workflow? (user action, scheduled event, external signal)
3. What data does the user provide at each step?
4. What data does the system return at each step?
5. What decisions does the user make along the way?
6. How does the workflow end? What's the final state?
```

### Data & State

```
1. What entities or objects does this feature create/modify?
2. What states can each entity be in? What transitions are allowed?
3. What data must be persisted? What's transient?
4. Is there a retention or archival policy for this data?
5. What existing data does this feature need to read?
```

### Error Paths

```
1. What happens when the user provides invalid data?
2. What happens when an external service is unavailable?
3. What happens on a timeout? Is the operation retryable?
4. What happens if two users do the same thing simultaneously?
5. What's the worst thing that could go wrong? How do we prevent it?
```

## Non-Functional Requirements (10 minutes)

### Performance

```
1. How many users will use this concurrently? (expected peak)
2. What response time is acceptable? (e.g., < 500ms P95)
3. How much data will this process? (volume, growth rate)
4. Are there batch operations? What's the expected batch size?
```

### Security & Compliance

```
1. What data is sensitive? (PII, financial, health)
2. Who should have access? Who should NOT?
3. Are there regulatory requirements? (PCI-DSS, SOX, GDPR)
4. Is multi-party approval required for any action?
5. What needs to be in the audit trail?
```

### Reliability

```
1. What happens if this feature is unavailable for 1 hour?
2. Is this feature on the critical path for revenue?
3. Does it need to work during database maintenance?
4. What's the recovery expectation if something fails?
```

## NexTruzt Escrow-Specific Questions

### Financial Transactions

```
1. What are the min/max transaction amounts?
2. What currencies are supported? Is conversion needed?
3. What are the escrow lifecycle states?
4. Who can initiate/approve each state transition?
5. What happens to funds during a dispute?
6. What timeout/expiration rules apply?
7. What's the settlement timeline (T+1, T+2)?
```

### Integration Points

```
1. Which payment gateway(s) are involved?
2. What KYC/AML checks are required?
3. Which notification channels (email, SMS, push)?
4. Are there webhooks or callbacks from external systems?
5. What reporting or analytics are needed?
```

## Workshop Closing (5 minutes)

```
1. What are the open questions we couldn't answer today?
2. Who owns answering each open question?
3. What's the minimum viable version of this feature?
4. What can be deferred to a later phase?
5. When do we reconvene to review the specification?
```

## Workshop Output Checklist

After the workshop, verify you captured:

- [ ] Feature name and one-line description
- [ ] Primary user roles and their goals
- [ ] Step-by-step workflow (happy path)
- [ ] At least 3 error/edge case scenarios
- [ ] Performance targets (response time, throughput)
- [ ] Security requirements (auth, audit, data protection)
- [ ] Open questions with assigned owners
- [ ] Agreed MVP scope vs. deferred items
