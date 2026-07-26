# Game Days Reference

> **Load when:** Planning and executing game day exercises with the engineering team.

## What is a Game Day?

A game day is a structured team exercise where chaos experiments are run in a controlled environment, with the engineering team actively observing, diagnosing, and responding to failures — building incident response muscle memory.

## Planning a Game Day

### 4-Week Preparation Timeline

```markdown
## Week 1: Define Scope and Objectives
- [ ] Choose the system(s) to test
- [ ] Define 3-5 specific failure scenarios
- [ ] Write hypotheses for each scenario
- [ ] Get management approval for production (if applicable)

## Week 2: Prepare Experiments
- [ ] Write experiment manifests (Litmus, Chaos Mesh, or manual scripts)
- [ ] Test experiments in staging
- [ ] Verify rollback procedures work
- [ ] Prepare monitoring dashboards

## Week 3: Prepare the Team
- [ ] Schedule the game day (avoid peak hours, end of sprint, or Friday afternoons)
- [ ] Brief all participants on objectives and rules
- [ ] Assign roles (see below)
- [ ] Set up communication channels (dedicated Slack channel, video call)
- [ ] Prepare runbook templates for each scenario

## Week 4: Execute and Document
- [ ] Run the game day
- [ ] Document observations in real-time
- [ ] Hold retrospective immediately after
- [ ] Publish findings and action items within 48 hours
```

### Roles and Responsibilities

| Role | Responsibility | Who |
|---|---|---|
| **Game Master** | Controls experiment execution, manages timeline | SRE / Platform engineer |
| **Observer** | Watches dashboards, logs, and traces; documents findings | On-call engineer |
| **Responder** | Diagnoses and resolves issues as if in a real incident | Application developer |
| **Scribe** | Records timeline, decisions, and observations | Any team member |
| **Safety Officer** | Monitors abort criteria; can halt experiment at any time | Senior engineer / TL |

## Game Day Scenarios for NexTruzt Escrow Platform

### Scenario 1: Payment Gateway Outage

```markdown
**Objective:** Validate circuit breaker and retry behavior when Stripe is unavailable
**Target:** Payment processing service
**Method:** Block outbound traffic to Stripe API for 60 seconds

**Experiment:**
1. T+0:00 — Inject: Block HTTPS traffic to api.stripe.com
2. T+0:00 — Observe: How quickly does the circuit breaker open?
3. T+0:30 — Observe: What happens to pending escrow creations?
4. T+1:00 — Remove: Unblock traffic
5. T+1:00 — Observe: How quickly does the system recover?

**Success Criteria:**
- Circuit breaker opens within 10 seconds
- User sees a friendly "Payment temporarily unavailable" message
- Pending payments are queued (not lost)
- Recovery within 30 seconds after restore
- Zero data inconsistency
```

### Scenario 2: Database Failover

```markdown
**Objective:** Validate PostgreSQL failover and application reconnection
**Target:** Primary PostgreSQL instance
**Method:** Promote read replica to primary, kill original primary

**Experiment:**
1. T+0:00 — Baseline: Record current read/write operations per second
2. T+0:30 — Inject: Promote replica to primary
3. T+0:30 — Observe: Application connection errors and reconnection
4. T+1:30 — Observe: Query routing to new primary
5. T+2:00 — Verify: Data consistency check

**Success Criteria:**
- Application reconnects within 30 seconds
- No data loss or corruption
- Read-only operations continue during failover
- Alerts fire correctly
```

### Scenario 3: Blazor Server Mass Disconnect

```markdown
**Objective:** Validate SignalR reconnection when load balancer drops connections
**Target:** Blazor Server SignalR connections
**Method:** Restart the Redis backplane (or SignalR hub service)

**Experiment:**
1. T+0:00 — Baseline: Count active circuits, note users on dashboards
2. T+0:30 — Inject: Restart Redis pub/sub service
3. T+0:30 — Observe: Blazor circuits disconnect, reconnection UI appears
4. T+1:00 — Observe: Circuits reconnect, state preserved
5. T+1:30 — Verify: User form data and navigation state intact

**Success Criteria:**
- Reconnection UI ("Reconnecting...") appears within 3 seconds
- 95%+ circuits reconnect automatically
- No user data lost (unsaved form data preserved)
- No duplicate transactions from retry logic
```

## Game Day Execution Template

### Pre-Game Checklist

```markdown
- [ ] All participants have joined the video call / Slack channel
- [ ] Monitoring dashboards are open and visible to all
- [ ] Experiment scripts are ready and tested in staging
- [ ] Rollback procedures are documented and accessible
- [ ] Abort criteria are agreed upon by all participants
- [ ] External dependencies are not in maintenance windows
- [ ] Customer support team is notified (if production)
- [ ] Incident response process is active (PagerDuty not in maintenance mode)
```

### Real-Time Documentation Template

```markdown
# Game Day Log — {Date}

## Participants
- Game Master: {name}
- Observer: {name}
- Responder: {name}
- Scribe: {name}
- Safety Officer: {name}

## Timeline

| Time | Event | Observed By | Notes |
|---|---|---|---|
| 14:00 | Game day started, baselines recorded | All | Error rate: 0.02%, p99: 120ms |
| 14:05 | Scenario 1 injected: Stripe blocked | Game Master | — |
| 14:05:12 | First payment error logged | Observer | Expected |
| 14:05:18 | Circuit breaker opened | Observer | ✅ Within 10s target |
| 14:06 | Customer-facing error: "Payment unavailable" | Responder | ✅ Friendly message |
| 14:06:00 | Stripe traffic restored | Game Master | — |
| 14:06:15 | Circuit breaker half-open, probing | Observer | — |
| 14:06:25 | Circuit breaker closed, normal operation | Observer | ✅ 25s recovery |
| 14:10 | Scenario 1 complete — moving to Scenario 2 | Game Master | — |
```

## Post-Game Day Retrospective

### Template

```markdown
# Game Day Retrospective — {Date}

## Summary
- **Scenarios Run:** {N} of {N planned}
- **Hypotheses Confirmed:** {N}
- **Hypotheses Failed:** {N}
- **Critical Findings:** {N}

## What Went Well
- {Positive finding 1 — e.g., circuit breakers worked as designed}
- {Positive finding 2 — e.g., team diagnosed the issue within 2 minutes}
- {Positive finding 3 — e.g., monitoring caught the problem immediately}

## What Surprised Us
- {Surprise 1 — e.g., health checks didn't fail even though service was degraded}
- {Surprise 2 — e.g., retry storms caused more load than the original failure}
- {Surprise 3 — e.g., alerts fired but went to the wrong channel}

## Action Items

| # | Action | Owner | Deadline | Priority |
|---|--------|-------|----------|----------|
| 1 | Fix health check to include downstream dependency status | {name} | {date} | High |
| 2 | Add rate limiting to retry logic to prevent retry storms | {name} | {date} | High |
| 3 | Update alert routing rules to correct Slack channel | {name} | {date} | Medium |
| 4 | Add Blazor reconnection smoke test to CI/CD | {name} | {date} | Medium |
| 5 | Schedule follow-up game day to verify fixes | {name} | {date} | Low |

## Metrics Comparison

| Metric | Baseline | During Chaos | Recovery | Target |
|---|---|---|---|---|
| Error rate | 0.02% | 2.1% | 0.05% | < 1% |
| P99 latency | 120ms | 3.2s | 150ms | < 2s |
| Recovery time | — | — | 25s | < 60s |

## Next Game Day
- **Date:** {planned date}
- **Scenarios:** {what we'll test next time}
- **Focus areas:** {what we want to improve based on this game day}
```

## Game Day Frequency

| Team Maturity | Recommended Frequency | Environment |
|---|---|---|
| **Getting started** | Quarterly | Staging only |
| **Building confidence** | Monthly | Staging + limited production |
| **Mature practice** | Weekly automated + Monthly manual | Full production |

Start with staging game days, graduate to production as the team builds confidence and the system proves resilient.
