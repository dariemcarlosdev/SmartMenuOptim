# Client Service Adapter Pattern

> **Pattern Type:** Structural / Adapter  
> **Applied In:** SmartMenuOptim.Server

---

## Overview

The Client Service Adapter Pattern wraps HTTP API calls behind a service interface, translating HTTP responses into domain-friendly Result objects. This isolates components from HTTP concerns and provides consistent error handling.

---

## Problem It Solves

| Problem | Solution |
|---------|----------|
| HTTP logic scattered in components | Centralized in service classes |
| Inconsistent error handling | Uniform Result pattern responses |
| Hard to test components | Mock service interface easily |
| Tight coupling to HTTP details | Components depend on abstraction |
| Duplicate API call code | Single implementation per entity |

---

## Implementation

### Interface Definition

**File:** `Services/Interfaces/IRestaurantClientService.cs`

```csharp
/// <summary>
/// Client service interface for Restaurant operations.
/// Abstracts HTTP API calls behind a clean interface.
/// </summary>
public interface IRestaurantClientService
{
    Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RestaurantDTO>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<RestaurantDTO>> CreateAsync(RestaurantCreateDTO dto, CancellationToken ct = default);
    Task<Result<RestaurantDTO>> UpdateAsync(RestaurantUpdateDTO dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> ToggleAcceptingOrdersAsync(int id, bool isAccepting, CancellationToken ct = default);
}
```

### Service Implementation

**File:** `Services/RestaurantClientService.cs`

```csharp
public class RestaurantClientService : IRestaurantClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RestaurantClientService> _logger;
    private const string ApiBasePath = "api/v1/restaurants";

    public RestaurantClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<RestaurantClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"{ApiBasePath}/{id}", ct);

            if (response.IsSuccessStatusCode)
            {
                var entity = await response.Content.ReadFromJsonAsync<RestaurantDTO>(ct);
                return entity is not null
                    ? Result.Success(entity)
                    : Result.Failure<RestaurantDTO>("Restaurant not found.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to load restaurant.");
            return Result.Failure<RestaurantDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading restaurant {Id}", id);
            return Result.Failure<RestaurantDTO>("Unable to connect to the server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading restaurant {Id}", id);
            return Result.Failure<RestaurantDTO>("An unexpected error occurred.");
        }
    }

    public async Task<Result<RestaurantDTO>> CreateAsync(RestaurantCreateDTO dto, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync(ApiBasePath, dto, ct);

            if (response.IsSuccessStatusCode)
            {
                var entity = await response.Content.ReadFromJsonAsync<RestaurantDTO>(ct);
                return entity is not null
                    ? Result.Success(entity)
                    : Result.Failure<RestaurantDTO>("Failed to create restaurant.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to create restaurant.");
            return Result.Failure<RestaurantDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating restaurant");
            return Result.Failure<RestaurantDTO>("Unable to connect to the server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating restaurant");
            return Result.Failure<RestaurantDTO>("An unexpected error occurred.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"{ApiBasePath}/{id}", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Restaurant {Id} deleted", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete restaurant.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting restaurant {Id}", id);
            return Result.Failure("Unable to connect to the server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting restaurant {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }
}
```

---

## Standard Method Template

```csharp
public async Task<Result<TDto>> OperationAsync(TInput input, CancellationToken ct = default)
{
    try
    {
        // 1. Get configured HTTP client
        var client = _httpClientFactory.CreateClient("BackendAPI");
        
        // 2. Make HTTP request
        var response = await client.GetAsync($"{ApiBasePath}/{id}", ct);

        // 3. Handle success
        if (response.IsSuccessStatusCode)
        {
            var entity = await response.Content.ReadFromJsonAsync<TDto>(ct);
            return entity is not null
                ? Result.Success(entity)
                : Result.Failure<TDto>("Entity not found.");
        }

        // 4. Handle API error (4xx, 5xx)
        var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Default error message.");
        return Result.Failure<TDto>(error);
    }
    catch (HttpRequestException ex)
    {
        // 5. Handle network/connection errors
        _logger.LogError(ex, "HTTP error in operation");
        return Result.Failure<TDto>("Unable to connect to the server.");
    }
    catch (Exception ex)
    {
        // 6. Handle unexpected errors
        _logger.LogError(ex, "Unexpected error in operation");
        return Result.Failure<TDto>("An unexpected error occurred.");
    }
}
```

---

## HTTP Methods Mapping

| Operation | HTTP Method | Code |
|-----------|-------------|------|
| Get by ID | GET | `client.GetAsync($"{path}/{id}", ct)` |
| Get all | GET | `client.GetFromJsonAsync<List<T>>(path, ct)` |
| Create | POST | `client.PostAsJsonAsync(path, dto, ct)` |
| Update | PUT | `client.PutAsJsonAsync($"{path}/{id}", dto, ct)` |
| Partial Update | PATCH | `client.PatchAsync($"{path}/{id}", content, ct)` |
| Delete | DELETE | `client.DeleteAsync($"{path}/{id}", ct)` |

---

## Error Helper

**File:** `Helpers/ApiErrorHelper.cs`

```csharp
public static class ApiErrorHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Extracts error message from HTTP response (RFC 7807 ProblemDetails format).
    /// </summary>
    public static async Task<string> GetErrorMessageAsync(
        HttpResponseMessage response, 
        string fallbackMessage)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return fallbackMessage;

            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(content, JsonOptions);
            return problemDetails?.Detail ?? problemDetails?.Title ?? fallbackMessage;
        }
        catch
        {
            return fallbackMessage;
        }
    }
}
```

---

## Service Registration

```csharp
public static IServiceCollection AddAppServices(this IServiceCollection services)
{
    // Register as Scoped (one instance per request/circuit)
    services.AddScoped<IRestaurantClientService, RestaurantClientService>();
    
    return services;
}
```

---

## Usage in State Container

```csharp
public class RestaurantDetailState : ComponentStateBase<RestaurantDTO>
{
    private readonly IRestaurantClientService _service;

    public async Task LoadAsync(int id, CancellationToken ct = default)
    {
        SetLoading();

        // Service returns Result<T> - no HTTP concerns here
        var result = await _service.GetByIdAsync(id, ct);

        if (result.IsSuccess && result.Value is not null)
            SetData(result.Value);
        else
            SetError(result.Error ?? "Not found.");
    }
}
```

---

## Architecture Flow

```
┌────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Component    │     │  State Container │     │ Client Service  │
│                │     │                  │     │                 │
│ Renders UI     │     │ Manages state    │     │ HTTP calls      │
│ Handles events │────▶│ Business logic   │────▶│ Error handling  │
│                │     │                  │     │ JSON mapping    │
└────────────────┘     └──────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
                                               ┌─────────────────┐
                                               │   Backend API   │
                                               │                 │
                                               │ REST Endpoints  │
                                               └─────────────────┘
```

---

## Benefits

| Benefit | Description |
|---------|-------------|
| **Abstraction** | Components don't know about HTTP |
| **Testability** | Easy to mock interface in tests |
| **Consistency** | Uniform error handling across all calls |
| **Maintainability** | API changes isolated to service layer |
| **Reusability** | Same service used by multiple components |
| **Logging** | Centralized logging for all API calls |

---

## Testing

### Mock Service for Unit Tests

```csharp
public class MockRestaurantClientService : IRestaurantClientService
{
    public Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dto = new RestaurantDTO { Id = id, Name = "Test Restaurant" };
        return Task.FromResult(Result.Success(dto));
    }

    // ... other methods
}
```

### Using Moq

```csharp
var mockService = new Mock<IRestaurantClientService>();
mockService
    .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Result.Success(new RestaurantDTO { Id = 1, Name = "Test" }));

var state = new RestaurantDetailState(mockService.Object, mockLogger);
await state.LoadAsync(1);

Assert.True(state.HasData);
Assert.Equal("Test", state.Restaurant?.Name);
```

---

## When to Use

✅ **Use When:**
- Consuming REST APIs from Blazor
- Need consistent error handling
- Want testable service layer
- Multiple components access same API

❌ **Avoid When:**
- Direct database access (use repositories instead)
- Very simple single-use API calls
- Real-time connections (use SignalR services)

---

## Related Patterns

- [Response/Result Pattern](./RESPONSE_RESULT_PATTERN.md) - Return type for operations
- [State Container Pattern](./STATE_CONTAINER_PATTERN.md) - Consumer of client services

---

## References

- [IHttpClientFactory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests)
- [Adapter Pattern](https://refactoring.guru/design-patterns/adapter)

---

*Document Version: 1.0*  
*Last Updated: 2025-03-01*
