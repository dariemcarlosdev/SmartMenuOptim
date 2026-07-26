---
name: test-generator
description: "Generate comprehensive unit and integration tests with edge cases, mocks, and parameterized scenarios — trigger: generate tests, write tests, create test file"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: testing
  triggers: generate tests, write tests, create test file, unit tests, integration tests, edge case tests
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: tdd-coach, test-coverage-analyzer
---

# Test Generator

Generate comprehensive, ready-to-run test suites covering happy paths, edge cases, error paths, and concurrency scenarios using Arrange-Act-Assert with proper mocking.

## When to Use This Skill

- When writing tests for new code with no test coverage
- When adding tests to legacy code before refactoring
- When you need edge case and error path tests that are easy to overlook
- When setting up mock infrastructure for a class with many dependencies
- After fixing a bug — to write a regression test preventing recurrence

## Core Workflow

1. **Analyze Code Under Test** — Read source, identify class purpose, constructor dependencies, namespace, domain context
   - ✅ Checkpoint: Class name, dependencies, and public API surface documented

2. **Map Public API Surface** — List public methods with parameters, return types, side effects, preconditions
   - ✅ Checkpoint: Every public method cataloged

3. **Generate Unit Tests** — Happy path, edge cases, error paths per method → See `references/unit-testing.md`
   - ✅ Checkpoint: ≥1 happy + ≥1 edge + ≥1 error test per method

4. **Generate Integration Tests** (if applicable) — WebApplicationFactory or TestContainers tests → See `references/integration-testing.md`
   - ✅ Checkpoint: Critical API endpoints have integration coverage

5. **Apply Assertion Best Practices** — FluentAssertions patterns → See `references/assertion-patterns.md`

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Unit Testing | `references/unit-testing.md` | xUnit, Moq, NSubstitute patterns |
| Integration Testing | `references/integration-testing.md` | WebApplicationFactory, TestContainers |
| Assertion Patterns | `references/assertion-patterns.md` | FluentAssertions best practices |
| Test Data | `references/test-data.md` | AutoFixture, Bogus, test data strategies |

## Quick Reference

```csharp
public sealed class CreateEscrowHandlerTests
{
    private readonly Mock<IEscrowRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateEscrowHandler _sut;

    public CreateEscrowHandlerTests()
    {
        _sut = new CreateEscrowHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldCreateEscrow()
    {
        // Arrange
        var command = new CreateEscrowCommand(UserId.New(), UserId.New(), Money.From(1000m));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<EscrowTransaction>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Constraints

### MUST DO
- Follow **Arrange-Act-Assert** in every test with clear section separation
- Use descriptive names: `MethodName_WhenCondition_ShouldExpectedBehavior`
- Generate at least one happy path, one edge case, and one error path per public method
- Mock all external dependencies — tests must run without databases or APIs
- Generate complete, compilable test files — not pseudocode
- Match the project's existing test framework and style

### MUST NOT
- Test private methods directly — test through the public API
- Write tests that depend on execution order
- Use `Thread.Sleep` — use async patterns or test clocks
- Assert on implementation details unless verifying critical side effects
- Generate tests without assertions
- Mock the class under test — only mock its dependencies

## Output Template

```csharp
// Tests for: {ClassName} | Source: {path} | Framework: xUnit + Moq + FluentAssertions

#region Happy Path Tests
[Fact] public async Task {Method}_WhenValidInput_ShouldReturnExpected() { }
#endregion

#region Edge Case Tests
[Theory] [InlineData(null)] [InlineData("")]
public async Task {Method}_WhenInvalidInput_ShouldThrow(string? input) { }
#endregion

#region Error Path Tests
[Fact] public async Task {Method}_WhenDependencyThrows_ShouldHandleGracefully() { }
#endregion
```
