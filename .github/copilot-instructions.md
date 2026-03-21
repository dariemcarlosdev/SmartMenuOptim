# Copilot Instructions for SmartMenuOptimizer

## Code Review

### Security
- Flag hardcoded secrets, API keys, and connection strings
- Identify insecure input handling and validation issues
- Check for proper authentication and authorization in Blazor components
- Verify secure API endpoint configurations

### Code Style
- Ensure all public methods and classes have XML documentation comments (`///`)
- Follow [Conventional Commits](https://www.conventionalcommits.org) specification
- Maintain consistent naming conventions (PascalCase for public members, camelCase for private)
- Use modern C# features (pattern matching, null-coalescing, record types)
- Prefer nullable reference types

### Logic & Design
- Identify missing edge cases and null reference risks
- Flag redundant loops and complex conditional checks
- Ensure error handling follows the project's Result pattern
- Verify adherence to Clean Architecture and DDD principles
- Check for proper separation of concerns across layers

### Testing
- Require unit tests for new business logic
- Suggest specific test cases for edge cases and failure scenarios
- Ensure Blazor components have appropriate tests
- Verify test coverage for critical paths

### Review Process
When performing code reviews:
- **Identify Issues**: Highlight potential issues inline as comments
- **Risk Assessment**: Include risk level: Very Low, Low, Medium, High, or Very High
- **Provide Suggestions**: Offer concrete, actionable improvements (not just critiques)
- **Context Awareness**: Ensure suggestions align with existing codebase patterns
- **Non-Blocking**: Remember that reviews are advisory and don't block merges

---

## Commit Messages

Follow the [Conventional Commits](https://www.conventionalcommits.org) specification:

### Format
```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `ci`: CI/CD changes

### Rules
- Use the imperative mood (e.g., "Add feature" not "Added feature")
- Limit the subject line to 50 characters
- Capitalize the subject line
- Do not end the subject line with a period
- Include a blank line before the body if additional context is needed
- Wrap the body at 72 characters
- Use bullet points for multiple changes in the body

### Examples
```
feat(menu): add optimization algorithm for menu items

fix(api): resolve null reference in MenuService

docs(readme): update setup instructions for .NET 9

refactor(domain): apply DDD patterns to Menu aggregate
```

---

## Code Generation

### Reference Implementation
- The **Restaurant Management module** is the canonical reference implementation for all feature modules
- Before generating code for any feature layer, consult `docs/08-Patterns/REFERENCE_IMPLEMENTATION_GUIDE.md`
- Match the Restaurant module's file structure, naming, documentation style, and error handling patterns
- Use mutable POCOs (not records) for DTOs — required for Blazor two-way binding
- Follow the established phase order: Domain → EF/Infra → Event Handlers → DTOs & Service → API → Blazor UI → Integration

### .NET/Blazor Specific
- Target .NET 8 or .NET 9 as appropriate
- Use nullable reference types throughout
- Prefer record types for DTOs and value objects
- Use dependency injection for all services
- Follow Blazor component lifecycle best practices
- Implement proper component parameter validation

### Clean Architecture & DDD
- Maintain strict separation of concerns across layers
- Keep domain logic pure in the Domain layer
- Use repository pattern for data access
- Implement aggregate roots with proper boundaries
- Use value objects for domain concepts without identity
- Keep application services thin (orchestration only)

### Interface Placement Rules
Interfaces must be defined in the layer that **needs** the abstraction:

**Domain layer** (`Domain/Repositories/`, `Domain/Services/Abstractions/`):
- Repository contracts (`IRepository<T>`, `IUnityOfWork`)
- Domain service abstractions (`ISentimentAnalyzer`, `IMenuCompositionValidator`)
- These express what the **domain needs** to function

**Application layer** (`Application/Contracts/`, `Application/Features/*/Services/`):
- Infrastructure ports (`ICacheService`, `IExternalPricingApi`, `IEmailService`)
- Application service contracts (`IRestaurantService`, `IMenuService`)
- These define what the **application orchestrates** — caching, external APIs, use cases

**Presentation layer** (`Server/Features/*/Services/`):
- Client adapters (`IRestaurantClientService`)
- These adapt backend services for **UI consumption** via HTTP

**Never** move infrastructure concerns (caching, external APIs) into Domain.
**Never** define repository interfaces in Application — they belong to Domain.

### Vertical Slice (Feature Folder) Conventions
- Organize feature code under `Features/{FeatureName}/` in each project layer
- Use **plural** for feature namespace (`Features.Restaurants`) to avoid C# namespace-type collision with singular class names (`Restaurant`)
- Cross-cutting concerns (base classes, shared contracts, middleware) stay in their original locations
- Use `GlobalDtoUsings.cs` for backward-compatible type aliases when migrating DTOs to feature folders
- EF Core configurations auto-discovered via `ApplyConfigurationsFromAssembly` regardless of folder

### Error Handling
- Use the Result pattern for operations that can fail
- Avoid throwing exceptions for expected failures
- Provide meaningful, user-friendly error messages
- Log errors with appropriate context and severity levels
- Handle async operations with proper cancellation token support

---

## Documentation

When needing the current date for documentation, use the PowerShell command `Get-Date -Format "yyyy-MM-dd"` to get the accurate current date from the system.
