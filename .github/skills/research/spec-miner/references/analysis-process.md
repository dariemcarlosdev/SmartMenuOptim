# Analysis Process

Step-by-step process for reverse-engineering specifications from existing code.

## Phase 1: Reconnaissance (30 minutes)

### Project Structure Scan

```bash
# 1. Solution and project layout
dotnet sln list
find . -name "*.csproj" | sort

# 2. Directory classification
find . -type d -maxdepth 3 | grep -v "obj\|bin\|node_modules\|.git"

# 3. Configuration files
cat global.json Directory.Build.props appsettings.json 2>/dev/null

# 4. File count per directory (identify concentration areas)
find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | \
  sed 's|/[^/]*$||' | sort | uniq -c | sort -rn | head -20
```

### Technology Stack Identification

```bash
# NuGet packages (reveals framework choices)
grep -rn "PackageReference" --include="*.csproj" | \
  sed 's/.*Include="\([^"]*\)" Version="\([^"]*\)".*/\1 \2/' | sort -u

# .NET version
grep -rn "TargetFramework" --include="*.csproj"

# Entry point analysis
cat **/Program.cs 2>/dev/null | head -50
```

## Phase 2: Domain Model Discovery (1 hour)

### Entity Extraction

```bash
# Find all domain entities
grep -rn "class.*: BaseEntity\|class.*: Entity\|class.*: AggregateRoot" \
  --include="*.cs"

# Find value objects
grep -rn "class.*: ValueObject\|record.*:" --include="*.cs" \
  Domain/ 2>/dev/null

# Find enums (state definitions)
grep -rn "enum " --include="*.cs" Domain/ 2>/dev/null

# Find domain events
grep -rn "class.*: IDomainEvent\|class.*: DomainEvent\|: INotification" \
  --include="*.cs"
```

### Relationship Mapping

```bash
# EF Core configurations reveal relationships
grep -rn "HasOne\|HasMany\|HasForeignKey\|OwnsOne\|OwnsMany" \
  --include="*.cs"

# Navigation properties
grep -rn "public.*virtual.*ICollection\|public.*virtual.*IReadOnlyList" \
  --include="*.cs"
```

## Phase 3: Business Logic Extraction (1 hour)

### MediatR Handlers = Use Cases

```bash
# Each handler IS a use case specification
grep -rn "class.*Handler.*: IRequestHandler" --include="*.cs"

# Extract command/query definitions (the API contract)
grep -rn "record.*Command\|record.*Query\|class.*Command\|class.*Query" \
  --include="*.cs"
```

### Validation Rules = Business Constraints

```bash
# FluentValidation rules document business constraints
grep -rn "RuleFor\|Must\|InclusiveBetween\|MaximumLength\|NotEmpty" \
  --include="*.cs"

# Domain invariants (Guard clauses in entities)
grep -rn "throw.*ArgumentException\|throw.*InvalidOperationException\|Guard\." \
  --include="*.cs" Domain/
```

### State Machine Discovery

```bash
# Find status/state enums
grep -B2 -A10 "enum.*Status\|enum.*State" --include="*.cs"

# Find state transitions
grep -rn "Status =\|State =\|ChangeStatus\|Transition" --include="*.cs"
```

## Phase 4: Integration Point Mapping (30 minutes)

```bash
# External HTTP clients
grep -rn "HttpClient\|AddHttpClient\|IHttpClientFactory" --include="*.cs"

# Message/event publishers
grep -rn "IPublisher\|Publish\|IMediator.*Publish" --include="*.cs"

# Background services
grep -rn "BackgroundService\|IHostedService" --include="*.cs"

# Database providers
grep -rn "UseSqlServer\|UseNpgsql\|UseSqlite" --include="*.cs"
```

## Phase 5: Specification Assembly

### Convert Findings to EARS Format

```
Code Finding:
  Entity: Escrow { Status: Pending → Funded → Released }
  Handler: FundEscrowHandler validates amount matches
  Validator: Amount > 0, Currency in ["USD", "EUR", "GBP"]

EARS Requirements:
  REQ-001: When a buyer submits a valid escrow request,
           the system shall create an escrow with status "Pending".
  REQ-002: When a buyer deposits funds matching the escrow amount,
           the system shall change status to "Funded".
  REQ-003: If the deposit amount does not match the escrow amount,
           then the system shall reject the deposit with error "Amount mismatch".
```

## Output: Discovered Specification Skeleton

```markdown
# Discovered Specification: {Module/Feature Name}

## Entities: {count} discovered
## Use Cases: {count} handlers found
## Business Rules: {count} validation rules + invariants
## Integration Points: {count} external dependencies
## State Machines: {count} status enums with transitions
```
