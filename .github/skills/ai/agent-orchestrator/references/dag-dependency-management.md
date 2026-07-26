# DAG Dependency Management Reference

> **Load when:** Managing task dependencies between agents, tracking work unit status, ordering execution waves.

## Dependency DAG Concepts

A Directed Acyclic Graph (DAG) ensures work units execute in the correct order — no agent starts before its prerequisites are complete.

```
            ┌──────────────┐
            │ analyze-domain│ (Wave 1)
            └──────┬───────┘
                   │
    ┌──────────────┼──────────────┐
    ▼              ▼              ▼
┌──────────┐ ┌──────────┐ ┌──────────────┐
│analyze-  │ │analyze-  │ │ analyze-     │ (Wave 1, parallel)
│auth      │ │infra     │ │ payments     │
└────┬─────┘ └────┬─────┘ └──────┬───────┘
     │            │              │
     └──────┬─────┘              │
            ▼                    │
     ┌─────────────┐            │
     │ design-plan │ ◄──────────┘ (Wave 2, depends on all Wave 1)
     └──────┬──────┘
            ▼
     ┌─────────────┐
     │ critic-plan │ (Wave 3, depends on design-plan)
     └──────┬──────┘
            ▼
     ┌─────────────┐
     │ implement   │ (Wave 4, depends on critic approval)
     └──────┬──────┘
            ▼
     ┌─────────────┐
     │ run-tests   │ (Wave 5, depends on implementation)
     └─────────────┘
```

## SQL-Based DAG Tracking

### Schema Setup

The `todos` and `todo_deps` tables are pre-existing in the session database.

```sql
-- todos table (pre-existing):
--   id TEXT PRIMARY KEY
--   title TEXT NOT NULL
--   description TEXT
--   status TEXT DEFAULT 'pending'  -- pending | in_progress | done | blocked
--   created_at TIMESTAMP
--   updated_at TIMESTAMP

-- todo_deps table (pre-existing):
--   todo_id TEXT (references todos.id)
--   depends_on TEXT (references todos.id)
--   PRIMARY KEY (todo_id, depends_on)
```

### Inserting a Work Plan

```sql
-- Insert work units
INSERT INTO todos (id, title, description, status) VALUES
  ('analyze-domain', 'Analyze domain layer', 
   'Explore entities, aggregates, value objects in src/Domain/. Report: entity list, relationships, invariants.', 'pending'),
  ('analyze-auth', 'Analyze auth module',
   'Review JWT config, policies, claims in src/Infrastructure/Identity/. Report: policies, scopes, token settings.', 'pending'),
  ('analyze-infra', 'Analyze infrastructure',
   'Review EF Core config, repositories in src/Infrastructure/. Report: DbContext setup, migrations, connection.', 'pending'),
  ('design-plan', 'Design implementation plan',
   'Based on analysis results, design the escrow release feature: handler, validator, endpoint, tests.', 'pending'),
  ('critic-review', 'Get critic review',
   'Submit plan to critic agent for validation. Address all blocking feedback.', 'pending'),
  ('implement', 'Implement feature',
   'Create handler, validator, endpoint in Application and Presentation layers.', 'pending'),
  ('run-tests', 'Run build and tests',
   'Execute dotnet build && dotnet test. Report pass/fail status.', 'pending');

-- Insert dependency edges
INSERT INTO todo_deps (todo_id, depends_on) VALUES
  ('design-plan', 'analyze-domain'),
  ('design-plan', 'analyze-auth'),
  ('design-plan', 'analyze-infra'),
  ('critic-review', 'design-plan'),
  ('implement', 'critic-review'),
  ('run-tests', 'implement');
```

### The Ready Query

This is the most important query — it finds all work units whose dependencies are complete.

```sql
-- Find todos with no pending dependencies (ready to execute)
SELECT t.id, t.title, t.description
FROM todos t
WHERE t.status = 'pending'
AND NOT EXISTS (
    SELECT 1 FROM todo_deps td
    JOIN todos dep ON td.depends_on = dep.id
    WHERE td.todo_id = t.id AND dep.status != 'done'
);
```

**Result after initial insert:** `analyze-domain`, `analyze-auth`, `analyze-infra` (all have no dependencies).

**Result after Wave 1 completes:** `design-plan` (all three analysis units are now `done`).

### Status Workflow

```
pending ──→ in_progress ──→ done
   │                          
   └──→ blocked (with reason in description)
```

```sql
-- Start working on a unit
UPDATE todos SET status = 'in_progress', updated_at = CURRENT_TIMESTAMP 
WHERE id = 'analyze-domain';

-- Complete a unit
UPDATE todos SET status = 'done', updated_at = CURRENT_TIMESTAMP 
WHERE id = 'analyze-domain';

-- Block a unit (with reason)
UPDATE todos SET status = 'blocked', 
  description = description || ' BLOCKED: Agent failed, need to retry.',
  updated_at = CURRENT_TIMESTAMP 
WHERE id = 'implement';
```

### Progress Dashboard Query

```sql
-- Overall progress
SELECT 
    status,
    COUNT(*) as count,
    GROUP_CONCAT(id, ', ') as units
FROM todos 
GROUP BY status;

-- Gantt-style view (order by wave)
SELECT 
    t.id,
    t.status,
    COALESCE(MAX(dep.status), 'none') as deepest_dep_status,
    COUNT(td.depends_on) as dep_count
FROM todos t
LEFT JOIN todo_deps td ON td.todo_id = t.id
LEFT JOIN todos dep ON td.depends_on = dep.id
GROUP BY t.id, t.status
ORDER BY dep_count ASC, t.id;
```

## Wave Execution Pattern

### Dispatch Algorithm

```
WHILE there are pending todos:
    1. Run the ready query → get list of executable units
    2. For each ready unit:
       a. Update status to 'in_progress'
       b. Launch appropriate agent
       c. Record agent_id in description
    3. Wait for agents to complete (notification-driven)
    4. For each completed agent:
       a. Read results
       b. Validate output
       c. Update status to 'done' (or 'blocked' on failure)
    5. Loop to find next wave of ready units
```

### Example Execution Trace

```
Wave 1: Ready = [analyze-domain, analyze-auth, analyze-infra]
  → Launch 3 explore agents in parallel
  → All complete → mark done

Wave 2: Ready = [design-plan]
  → Do it yourself (simple synthesis task)
  → Mark done

Wave 3: Ready = [critic-review]
  → Launch critic agent
  → Complete with feedback → mark done

Wave 4: Ready = [implement]
  → Launch general-purpose agent
  → Complete → mark done

Wave 5: Ready = [run-tests]
  → Launch task agent
  → Complete → mark done

All todos done → aggregate results and report
```

## Dependency Validation

### Cycle Detection

Before executing, verify the DAG has no cycles.

```sql
-- Simple cycle check: any todo that depends on itself (direct cycle)
SELECT td.todo_id, td.depends_on 
FROM todo_deps td 
WHERE td.todo_id = td.depends_on;

-- Transitive cycle detection (depth-limited)
WITH RECURSIVE dep_chain(todo_id, depends_on, depth) AS (
    SELECT todo_id, depends_on, 1 FROM todo_deps
    UNION ALL
    SELECT dc.todo_id, td.depends_on, dc.depth + 1
    FROM dep_chain dc
    JOIN todo_deps td ON dc.depends_on = td.todo_id
    WHERE dc.depth < 20
)
SELECT todo_id, depends_on, depth 
FROM dep_chain 
WHERE todo_id = depends_on;
-- If this returns rows, there's a cycle — fix the dependency graph
```

### Orphan Detection

```sql
-- Find todos that are depended on but don't exist
SELECT DISTINCT td.depends_on 
FROM todo_deps td 
LEFT JOIN todos t ON td.depends_on = t.id 
WHERE t.id IS NULL;
```

## Real-World Dependency Chains

### Feature Implementation Chain

```sql
INSERT INTO todos (id, title, status) VALUES
  ('explore-model', 'Explore domain model', 'pending'),
  ('explore-tests', 'Explore existing tests', 'pending'),
  ('write-handler', 'Write command handler', 'pending'),
  ('write-validator', 'Write FluentValidation', 'pending'),
  ('write-tests', 'Write unit tests', 'pending'),
  ('write-endpoint', 'Write API endpoint', 'pending'),
  ('integration-test', 'Run integration tests', 'pending');

INSERT INTO todo_deps (todo_id, depends_on) VALUES
  ('write-handler', 'explore-model'),
  ('write-validator', 'explore-model'),
  ('write-tests', 'write-handler'),
  ('write-tests', 'write-validator'),
  ('write-endpoint', 'write-handler'),
  ('integration-test', 'write-endpoint'),
  ('integration-test', 'write-tests');

-- Wave 1: explore-model, explore-tests (parallel)
-- Wave 2: write-handler, write-validator (parallel, both depend on explore-model)
-- Wave 3: write-tests, write-endpoint (parallel, different deps)
-- Wave 4: integration-test (depends on both Wave 3 units)
```
