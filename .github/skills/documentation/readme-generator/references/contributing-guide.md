# Contributing Guide Reference

Templates and conventions for generating CONTRIBUTING.md files for .NET / Clean Architecture projects.

## CONTRIBUTING.md Section Order

| # | Section | Description |
|---|---------|-------------|
| 1 | Welcome | Brief thank-you and link to Code of Conduct |
| 2 | Development Setup | Prerequisites, clone, build, run instructions |
| 3 | Branch Naming | Convention for feature, fix, and chore branches |
| 4 | Commit Messages | Conventional Commits format and examples |
| 5 | Pull Request Process | How to open, describe, and get PRs reviewed |
| 6 | Code Review | What reviewers look for, expectations |
| 7 | Coding Standards | .NET conventions, architecture rules |
| 8 | Testing Requirements | What tests are required before merging |
| 9 | Issue Reporting | How to file bugs and feature requests |
| 10 | Getting Help | Where to ask questions |

## Development Setup

```markdown
## Development Setup

### Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download) |
| PostgreSQL | 16+ | [Download](https://www.postgresql.org/download/) or `docker compose up -d postgres` |
| Docker | 24.0+ | [Download](https://docs.docker.com/get-docker/) |
| Git | 2.40+ | [Download](https://git-scm.com/downloads) |

### First-Time Setup

```bash
# Clone the repository
git clone https://github.com/{owner}/{repo}.git
cd {repo}

# Restore dependencies
dotnet restore

# Start infrastructure
docker compose up -d postgres

# Apply database migrations
dotnet ef database update --project src/{Repo}.Infrastructure

# Run the application
dotnet run --project src/{Repo}.Web

# Run all tests
dotnet test
```

### IDE Recommendations

- **Visual Studio 2022** (17.12+) with ASP.NET and web development workload
- **JetBrains Rider** (2024.3+)
- **VS Code** with C# Dev Kit extension
```

## Branch Naming Conventions

```markdown
## Branch Naming

Use the following branch naming convention:

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/{issue-id}-{short-description}` | `feature/42-escrow-release-workflow` |
| Bug fix | `fix/{issue-id}-{short-description}` | `fix/87-null-ref-on-deposit` |
| Chore | `chore/{short-description}` | `chore/update-ef-core-10` |
| Documentation | `docs/{short-description}` | `docs/api-authentication-guide` |
| Refactor | `refactor/{short-description}` | `refactor/extract-payment-service` |
| Hotfix | `hotfix/{issue-id}-{short-description}` | `hotfix/102-escrow-timeout-fix` |

- Always branch from `main` (or `develop` if using GitFlow)
- Keep branch names lowercase with hyphens
- Include the issue number when applicable
```

## Commit Message Format

```markdown
## Commit Messages

This project follows [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | When to Use |
|------|-------------|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `docs` | Documentation changes only |
| `style` | Formatting, whitespace (no logic change) |
| `refactor` | Code restructuring (no feature or fix) |
| `perf` | Performance improvement |
| `test` | Adding or updating tests |
| `build` | Build system or dependency changes |
| `ci` | CI/CD configuration changes |
| `chore` | Maintenance tasks |

### Scopes

Use the Clean Architecture layer or feature area as the scope:

- `domain`, `application`, `infrastructure`, `web`
- Feature-specific: `escrow`, `payment`, `auth`, `notification`

### Examples

```
feat(escrow): add multi-party release approval workflow

Implements the release approval chain where all parties must
approve before funds are released. Uses domain events to
notify each participant.

Closes #42
```

```
fix(infrastructure): resolve connection pool exhaustion under load

Increased MaxPoolSize to 100 and added connection lifetime
rotation to prevent stale connections.

Fixes #87
```

```
test(application): add unit tests for CreateEscrowCommandHandler
```
```

## Pull Request Process

```markdown
## Pull Request Process

### Before Opening a PR

1. ✅ Code compiles without warnings: `dotnet build --warnaserror`
2. ✅ All tests pass: `dotnet test`
3. ✅ New code has tests (aim for ≥ 80% coverage on new code)
4. ✅ Branch is up-to-date with `main`: `git rebase main`
5. ✅ Commit messages follow Conventional Commits format

### PR Description Template

```markdown
## Summary
{Brief description of what this PR does}

## Motivation
{Why is this change needed? Link to issue: Closes #{issue}}

## Changes
- {List key changes}

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated (if applicable)
- [ ] Manual testing performed

## Screenshots (if UI changes)
{Before/after screenshots}
```

### Review Process

1. Open a draft PR early for complex changes to get early feedback
2. Request review from at least one team member
3. Address all review comments or explain why you disagree
4. Squash-merge to `main` after approval
5. Delete the feature branch after merge
```

## Code Review Expectations

```markdown
## Code Review

### What Reviewers Check

- **Correctness:** Does the code do what it claims? Edge cases handled?
- **Architecture:** Does it respect Clean Architecture boundaries?
  - Domain has no infrastructure dependencies
  - Application layer uses abstractions (interfaces) for external concerns
  - Infrastructure implements interfaces defined in Application
- **SOLID Principles:** Single responsibility, dependency inversion, etc.
- **Security:** Input validation, authorization checks, no exposed secrets
- **Performance:** N+1 queries, missing `CancellationToken`, unnecessary allocations
- **Tests:** Adequate coverage, meaningful assertions, no brittle tests
- **Naming:** Intention-revealing names, consistent conventions

### Review Etiquette

- Be constructive — suggest improvements, don't just criticize
- Use "nit:" prefix for non-blocking style suggestions
- Approve with minor comments when changes are trivial
- Request changes only for correctness, security, or architecture issues
```

## Coding Standards

```markdown
## Coding Standards

### C# Conventions

- File-scoped namespaces
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- `sealed` on classes not designed for inheritance
- `record` types for immutable DTOs
- Primary constructors where they improve clarity
- Expression-bodied members for single-line logic
- Guard clauses over nested conditionals

### Architecture Rules

- **Domain layer:** No dependencies on other layers, no NuGet packages (except primitives)
- **Application layer:** References only Domain; uses `IRepository<T>`, `IUnitOfWork` interfaces
- **Infrastructure layer:** Implements Application interfaces; contains EF Core, external service clients
- **Web layer:** References Application; uses MediatR to dispatch commands/queries
- **No circular dependencies** between projects

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| CQRS Command | `{Verb}{Entity}Command` | `CreateEscrowCommand` |
| CQRS Query | `Get{Entity}Query` | `GetEscrowByIdQuery` |
| Handler | `{Command/Query}Handler` | `CreateEscrowCommandHandler` |
| Validator | `{Command}Validator` | `CreateEscrowCommandValidator` |
| Entity | PascalCase noun | `EscrowTransaction` |
| Value Object | PascalCase noun | `Money`, `EscrowStatus` |
| Interface | `I{Name}` | `IEscrowRepository` |
```

## Testing Requirements

```markdown
## Testing

### Required Tests

| Change Type | Required Tests |
|-------------|---------------|
| New domain entity/logic | Unit tests for business rules and invariants |
| New command handler | Unit test with mocked dependencies |
| New query handler | Unit test verifying correct data projection |
| New validator | Tests for valid input, each validation rule, edge cases |
| New API endpoint | Integration test with `WebApplicationFactory` |
| Bug fix | Regression test that reproduces the bug |

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/EscrowApp.Domain.Tests

# With coverage (Coverlet)
dotnet test --collect:"XPlat Code Coverage"

# Filter by test name
dotnet test --filter "FullyQualifiedName~CreateEscrow"
```

### Test Conventions

- **Naming:** `{MethodUnderTest}_Should{ExpectedResult}_When{Condition}`
- **Pattern:** Arrange → Act → Assert
- **Mocking:** Use NSubstitute or Moq — prefer NSubstitute for readability
- **No infrastructure in unit tests** — mock all external dependencies
- **Integration tests** use `WebApplicationFactory<Program>` with a test database
```

## Complete CONTRIBUTING.md Template

```markdown
# Contributing to {ProjectName}

Thank you for your interest in contributing! This guide will help you get started.

Please read our [Code of Conduct](CODE_OF_CONDUCT.md) before contributing.

## Quick Start

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/42-my-feature`
3. Make your changes with tests
4. Commit using Conventional Commits: `git commit -m "feat(escrow): add release approval"`
5. Push and open a Pull Request

## Development Setup

### Prerequisites
- .NET SDK 10.0+
- PostgreSQL 16+ (or Docker)
- Docker & Docker Compose

### Setup
```bash
git clone https://github.com/{owner}/{repo}.git && cd {repo}
docker compose up -d postgres
dotnet restore && dotnet ef database update --project src/{Repo}.Infrastructure
dotnet run --project src/{Repo}.Web
dotnet test  # verify everything works
```

## Branch Naming
- `feature/{issue}-{description}` — new features
- `fix/{issue}-{description}` — bug fixes
- `docs/{description}` — documentation

## Commit Messages
Follow [Conventional Commits](https://www.conventionalcommits.org/):
- `feat(scope): description` — features
- `fix(scope): description` — bug fixes
- `test(scope): description` — tests
- `docs(scope): description` — documentation

## Pull Requests
1. Ensure `dotnet build --warnaserror` passes
2. Ensure `dotnet test` passes
3. Add tests for new functionality
4. Update documentation if needed
5. Request review from a maintainer

## Coding Standards
- Follow existing code style and conventions
- Respect Clean Architecture boundaries
- Use `sealed` classes, file-scoped namespaces, nullable enabled
- Name CQRS artifacts: `{Verb}{Entity}Command`, `{Command}Handler`, `{Command}Validator`

## Testing
- Unit tests for all new domain logic and handlers
- Integration tests for new API endpoints
- Regression tests for bug fixes
- Target ≥ 80% coverage on new code

## Need Help?
- Open a [Discussion](https://github.com/{owner}/{repo}/discussions) for questions
- Check existing [Issues](https://github.com/{owner}/{repo}/issues) for known problems
- Tag maintainers in your PR if you need guidance

Thank you for contributing! 🎉
```
