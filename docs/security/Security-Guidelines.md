# Security Guidelines for Multi-Tenancy

## Overview

This document outlines security best practices for implementing and maintaining the multi-tenant architecture in SmartMenuOptimizer. These guidelines ensure proper tenant isolation, data security, and access control.

## Core Security Principles

### 1. Tenant Isolation

#### Data Isolation
- **Mandatory Tenant ID**: Every tenant-specific entity must include `RestaurantId`
- **Global Query Filters**: Use EF Core query filters to automatically filter by tenant
- **Soft Deletion**: Implement soft delete patterns to maintain data history without exposing deleted records
- **Cross-Tenant Access Prevention**: Strictly enforce tenant boundaries at repository and database levels

#### Database Security
- Configure proper foreign key constraints within tenant scope
- Implement cascade delete only within tenant boundaries
- Use row-level security when available in the database platform
- Regularly audit database access patterns for potential isolation breaches

# ------------------------------------------------------------------------------------------------

### 2. Authentication and Authorization

#### Tenant Context Validation
- Validate tenant context in middleware for every request
- Store tenant information in secure claims
- Implement proper JWT handling with tenant-specific claims
- Use the `TenantAuthorizationHandler` for tenant-specific authorization

    public class TenantRequirement : IAuthorizationRequirement { }
    public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
    {
    private readonly IHttpContextAccessor httpContextAccessor;  
    private readonly ILogger<TenantAuthorizationHandler> logger;

    public TenantAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ILogger<TenantAuthorizationHandler> logger)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantRequirement requirement)
    {
        HttpContext httpContext = context.Resource as HttpContext ?? httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            logger.LogDebug("No HttpContext available while evaluating TenantRequirement.");
            return;
        }

        try
        {
            if (!httpContext.Items.TryGetValue("RestaurantId", out var restaurantIdObj) || !(restaurantIdObj is int restaurantId))
            {
                logger.LogDebug("RestaurantId missing or invalid in HttpContext.Items.");
                return;
            }

            var userRestaurantIds = await Task.Run(() => GetUserRestaurantIds(context.User)).ConfigureAwait(false);

            if (userRestaurantIds.Contains(restaurantId))
            {
                context.Succeed(requirement);
            }
            else
            {
                logger.LogInformation("User not authorized for restaurant {RestaurantId}.", restaurantId);
            }
        }
        catch (Exception ex)
        {
            var correlationId = httpContext.TraceIdentifier;
            logger.LogError(ex, "Unhandled exception while evaluating tenant authorization. CorrelationId: {CorrelationId}", correlationId);
            // Fail closed: do not call context.Succeed
        }
    }

    private static IReadOnlyCollection<int> GetUserRestaurantIds(ClaimsPrincipal user)
    {
        var claimValue = user?.FindFirst("restaurant_ids")?.Value ?? user?.FindFirst("restaurants")?.Value;
        if (string.IsNullOrWhiteSpace(claimValue)) return Array.Empty<int>();

        var ids = new List<int>();
        foreach (var part in claimValue.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out var id))
            {
                ids.Add(id);
            }
        }
        return ids;
    }
}

#### Access Control
- Implement role-based access control (RBAC) within tenant context
- Define clear permission hierarchies for admin users
- Regularly audit access logs for unauthorized attempts
- Implement proper session management and timeout policies

# ------------------------------------------------------------------------------------------------

### 3. API Security

#### Request Validation
- Validate tenant context in all API endpoints
- Implement rate limiting per tenant
- Use HTTPS for all communications
- Implement proper input validation and sanitization

Example middleware implementation:

#### Middleware for Tenant Context

- Implement middleware to extract and validate tenant context from requests

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var restaurantIdClaim = context.User.FindFirst("restaurant_id")?.Value;
            if (int.TryParse(restaurantIdClaim, out var restaurantId))
            {
                context.Items["RestaurantId"] = restaurantId;
                _logger.LogDebug("Set RestaurantId {RestaurantId} in HttpContext.Items.", restaurantId);
            }
            else
            {
                _logger.LogWarning("Invalid or missing restaurant_id claim for authenticated user.");
            }
        }
        await _next(context);
    }
}


# ------------------------------------------------------------------------------------------------

### 4. Data Protection

#### Sensitive Data Handling
- Encrypt sensitive tenant data at rest
- Implement proper key management per tenant
- Use secure audit logging for sensitive operations
- Implement data retention policies per tenant

#### Backup and Recovery
- Implement tenant-aware backup strategies
- Ensure tenant data isolation in backups
- Test restoration procedures regularly
- Document disaster recovery procedures


# ------------------------------------------------------------------------------------------------

## Best Practices Implementation

### 1. Repository Pattern

- Implement tenant-aware repositories

Example:

Public interface ITenantRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id, int restaurantId);
    Task<IEnumerable<T>> GetAllAsync(int restaurantId);
    Task AddAsync(T entity, int restaurantId);
    Task UpdateAsync(T entity, int restaurantId);
    Task DeleteAsync(int id, int restaurantId);
}

Public class TenantRepository<T> : ITenantRepository<T> where T : class
{
    private readonly DbContext _context;
    public TenantRepository(DbContext context)
    {
        _context = context;
    }
    public async Task<T> GetByIdAsync(int id, int restaurantId)
    {
        return await _context.Set<T>().FindAsync(id, restaurantId);
    }
    public async Task<IEnumerable<T>> GetAllAsync(int restaurantId)
    {
        return await _context.Set<T>().Where(e => EF.Property<int>(e, "RestaurantId") == restaurantId).ToListAsync();
    }
    public async Task AddAsync(T entity, int restaurantId)
    {
        _context.Entry(entity).Property("RestaurantId").CurrentValue = restaurantId;
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateAsync(T entity, int restaurantId)
    {
        _context.Entry(entity).Property("RestaurantId").CurrentValue = restaurantId;
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id, int restaurantId)
    {
        var entity = await GetByIdAsync(id, restaurantId);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

### 2. Security Checklist

#### Development Phase
- [ ] Implement tenant validation middleware
- [ ] Configure EF Core query filters
- [ ] Set up proper authorization handlers
- [ ] Implement audit logging
- [ ] Configure data encryption
- [ ] Implement rate limiting
- [ ] Set up secure session management

#### Deployment Phase
- [ ] Configure SSL/TLS
- [ ] Set up proper firewall rules
- [ ] Configure backup systems
- [ ] Set up monitoring and alerting
- [ ] Document security procedures
- [ ] Perform security testing

#### Maintenance Phase
- [ ] Regular security audits
- [ ] Monitor access patterns
- [ ] Review and update security policies
- [ ] Test disaster recovery procedures
- [ ] Update security documentation

# ------------------------------------------------------------------------------------------------

## Security Testing

### 1. Tenant Isolation Testing

[Fact] public async Task EnsureTenantIsolation() { // Arrange var tenant1Client = _factory.CreateClientForTenant(1); var tenant2Client = _factory.CreateClientForTenant(2);
// Act
var tenant1Response = await tenant1Client.GetAsync("/api/data");
var tenant2Response = await tenant2Client.GetAsync("/api/data");

tenant1Response.EnsureSuccessStatusCode();
tenant2Response.EnsureSuccessStatusCode();

var tenant1Data = await tenant1Response.Content.ReadFromJsonAsync<IEnumerable<DataDto>>();
var tenant2Data = await tenant2Response.Content.ReadFromJsonAsync<IEnumerable<DataDto>>();

// Assert
Assert.NotNull(tenant1Data);
Assert.NotNull(tenant2Data);
Assert.NotEqual(
    System.Text.Json.JsonSerializer.Serialize(tenant1Data),
    System.Text.Json.JsonSerializer.Serialize(tenant2Data));
}

### 2. Regular Security Assessments
- Perform penetration testing
- Conduct security code reviews
- Test tenant isolation regularly
- Validate authorization mechanisms
- Review audit logs for suspicious patterns

# ------------------------------------------------------------------------------------------------

## Incident Response

### 1. Security Breach Protocol
- Document incident response procedures
- Define communication channels
- Establish containment procedures
- Plan recovery steps
- Implement post-incident analysis

### 2. Tenant Data Protection
- Implement tenant data backup procedures
- Define data recovery processes
- Document tenant notification procedures
- Maintain incident logs per tenant

## Compliance and Auditing

### 1. Audit Trail Requirements
- Log all sensitive operations
- Maintain tenant-specific audit logs
- Implement audit log retention policies
- Regular audit log reviews

### 2. Compliance Documentation
- Maintain security documentation
- Document compliance requirements
- Regular compliance reviews
- Update security procedures as needed




