# Smart Menu Optimizer - Data Access and Security Guidelines

## Table of Contents
- [Multi-Tenant Data Access Patterns](#multi-tenant-data-access-patterns)
- [Security Implementation Requirements](#security-implementation-requirements)
- [Blazor Security Implementation](#blazor-security-implementation)
- [Best Practices](#best-practices)
- [Implementation Checklists](#implementation-checklists)

## Multi-Tenant Data Access Patterns

### Tenant-Specific Queries
All queries for restaurant-specific data MUST be filtered by `RestaurantId` to ensure tenant isolation.

public async Task<List<Dish>> GetDishesByRestaurantAsync(int restaurantId) { return await _context.Dishes .Where(d => d.RestaurantId == restaurantId) .ToListAsync(); }

### Global Entity Queries
Queries for global entities, such as `AdminUser`, do not require tenant filtering but MUST be restricted to authorized administrative contexts.

public async Task<AdminUser?> GetAdminUserAsync(int userId) { return await _context.AdminUsers .FirstOrDefaultAsync(a => a.Id == userId); }

## Security Implementation Requirements

### Request Validation Pipeline
1.  **Mandatory `RestaurantId` Validation**:
    *   Validate `RestaurantId` in all incoming requests.
    *   Perform validation as early as possible in the request pipeline.
    *   Use middleware for consistent validation across all endpoints.

2.  **Tenant Isolation Middleware**: Implement middleware to extract and validate the `RestaurantId` from the user's claims, rejecting any unauthorized requests.

    ```csharp
    public class RestaurantTenantMiddleware
    {
        private readonly RequestDelegate _next;

        public RestaurantTenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Example: This logic should be adapted to your app's specific needs
            if (context.User.Identity?.IsAuthenticated ?? false)
            {
                var restaurantIdClaim = context.User.FindFirst("RestaurantId")?.Value;
                if (string.IsNullOrEmpty(restaurantIdClaim))
                {
                    // For API endpoints, returning 401/403 might be appropriate
                    // For Blazor Server, a redirect might be better
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: Missing RestaurantId claim.");
                    return;
                }
            }
            await _next(context);
        }
    }
    ```

## Blazor Security Implementation

### Authentication Configuration
Configure authentication services in `Program.cs`.

//Program.cs builder.Services.AddAuthentication(options => { options.DefaultScheme = "Cookies"; options.DefaultChallengeScheme = "oidc"; // Or your chosen challenge scheme });

### Claims-Based Authorization

1.  **Custom Claims Principal (Optional but Recommended)**: Create a helper class to easily access custom claims.

    ```csharp
    public static class ClaimsPrincipalExtensions
    {
        public static int GetRestaurantId(this ClaimsPrincipal principal)
        {
            var restaurantIdValue = principal.FindFirst("RestaurantId")?.Value;
            return int.TryParse(restaurantIdValue, out var restaurantId) ? restaurantId : 0;
        }

        public static bool IsRestaurantAdmin(this ClaimsPrincipal principal)
        {
            return principal.HasClaim("RestaurantRole", "Admin");
        }
    }
    ```

2.  **Authorization Policies**: Define policies in `Program.cs` to enforce role and claim-based access control.

    ```csharp
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RestaurantUser", policy =>
            policy.RequireClaim("RestaurantId"));
        
        options.AddPolicy("RestaurantAdmin", policy =>
            policy.RequireClaim("RestaurantId")
                  .RequireClaim("RestaurantRole", "Admin"));
    });
    ```

### Component Security Implementation

1.  **Declarative Authorization**: Use the `AuthorizeView` component to conditionally render UI based on authorization policies.

    ```razor
    <AuthorizeView Policy="RestaurantAdmin">
        <Authorized>
            <RestaurantSettingsComponent RestaurantId="@context.User.GetRestaurantId()" />
        </Authorized>
        <NotAuthorized>
            <p>Access denied: You do not have sufficient permissions.</p>
        </NotAuthorized>
    </AuthorizeView>
    ```

2.  **Route Protection**: Protect entire pages by applying the `[Authorize]` attribute.

    ```razor
    @page "/restaurant-dashboard"
    @attribute [Authorize(Policy = "RestaurantUser")]
    
    <h3>Restaurant Dashboard</h3>
    ```

### Error Handling and State
Handle authentication state changes and potential errors within components.

[CascadingParameter] private Task<AuthenticationState> AuthState { get; set; }
private ClaimsPrincipal _user;
protected override async Task OnInitializedAsync() { var authState = await AuthState; _user = authState.User;
    if (!_user.Identity?.IsAuthenticated ?? true)
        {
        NavigationManager.NavigateTo("/login");
        }
}

## Best Practices

### Data Access
-   **Async Operations**: Always use `async/await` for database calls to prevent blocking threads.
-   **Repository Pattern**: Abstract data access logic into repositories for consistency and testability.
-   **Security Audits**: Regularly review data access patterns for potential security vulnerabilities.

### State Management
-   **Server-Side State**: In Blazor Server, store sensitive claims and state on the server.
-   **Secure Storage**: Avoid using browser local storage for sensitive information like tokens or personal data. Use secure session storage for temporary data if necessary.

### API Security
-   **Anti-Forgery Tokens**: Protect against CSRF attacks by including anti-forgery tokens in form posts.
-   **Typed HTTP Clients**: Use typed `HttpClient` instances configured with base addresses and default authorization headers.

### Performance
-   **Cache Decisions**: Cache authorization decisions where appropriate to reduce redundant checks.
-   **Lazy Loading**: Use lazy loading for components that are only accessible to certain authorized users.
-   **Efficient Lookups**: Design claim structures for efficient lookups.

## Implementation Checklists

### Core Security Setup
- [ ] Tenant validation middleware is implemented and registered.
- [ ] Claims-based authorization policies are defined.
- [ ] Repository methods correctly filter by `RestaurantId`.
- [ ] Access to global entities is properly restricted.
- [ ] Logging and monitoring for security events are in place.

### Blazor-Specific Implementation
- [ ] Authentication is configured in `Program.cs`.
- [ ] Authorization policies for Blazor are defined.
- [ ] `AuthorizeView` and `[Authorize]` attributes are used to protect components and routes.
- [ ] Authentication state is correctly handled in components.
- [ ] Secure state management practices are followed
