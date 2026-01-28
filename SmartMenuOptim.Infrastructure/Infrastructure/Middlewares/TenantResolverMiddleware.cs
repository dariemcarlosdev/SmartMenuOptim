using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace SmartMenuOptim.Infrastructure.Infrastructure.Middlewares
{
    /// <summary>
    /// TenantResolverMiddleware
    /// 
    /// Description:
    /// This middleware is designed for multi-tenant applications to resolve and identify the tenant context for each incoming HTTP request. It extracts the tenant identifier from one of several sources (header, query string, or subdomain) and makes it available to downstream components via HttpContext.Items. 
    /// This enables services, controllers, and other middleware to access tenant-specific data and logic.
    /// 
    /// Why use this middleware?
    /// - Ensures every request is associated with a tenant, which is essential for data isolation and security in multi-tenant systems.
    /// - Centralizes tenant resolution logic, reducing code duplication and potential errors across the application.
    /// 
    /// Where to use:
    /// - Register this middleware early in the ASP.NET Core pipeline (in Program.cs or Startup.cs) so that tenant information is available to all subsequent middleware and request handlers.
    /// 
    /// How to use:
    /// - Add to the middleware pipeline with: app.UseMiddleware<TenantResolverMiddleware>();
    /// - Access the resolved tenant ID in controllers or services via: context.Items["TenantId"]
    /// 
    /// Supports resolution from header (X-Tenant-ID), query string (tenantId), and subdomain (e.g., tenant1.example.com).
    /// </summary>
    public class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private const string TenantIdKey = "TenantId"; // Key for storing tenant info in HttpContext
        private const string TenantHeader = "X-Tenant-ID"; // Header name for tenant
        private const string TenantQuery = "tenantId"; // Query parameter name for tenant

        public TenantResolverMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        /// <summary>
        /// Extracts tenant information from header, query, or subdomain and sets it in HttpContext.Items.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            string? tenantId = null;

            // 1. Try to get tenant from header (case-insensitive)
            if (context.Request.Headers.TryGetValue(TenantHeader, out var headerTenant))
            {
                tenantId = headerTenant.FirstOrDefault();
            }

            // 2. If not found, try to get tenant from query string
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = context.Request.Query[TenantQuery].FirstOrDefault();
            }

            // 3. If still not found, try to extract tenant from subdomain (e.g., tenant1.example.com)
            if (string.IsNullOrEmpty(tenantId))
            {
                var host = context.Request.Host.Host;
                // Assumes subdomain is the first label (e.g., tenant1.example.com)
                var match = Regex.Match(host, @"^(?<tenant>[^.]+)\.");
                if (match.Success)
                {
                    tenantId = match.Groups["tenant"].Value;
                }
            }

            // 4. If tenantId is found, store it in HttpContext for downstream use
            if (!string.IsNullOrEmpty(tenantId))
            {
                context.Items[TenantIdKey] = tenantId;
                // Proceed to the next middleware
                await _next(context);
            }
            else
            {
                // 5. Handle missing tenant information (return error or set default)
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Tenant ID is required (header, query, or subdomain).");
                return;
            }
        }
    }
}
