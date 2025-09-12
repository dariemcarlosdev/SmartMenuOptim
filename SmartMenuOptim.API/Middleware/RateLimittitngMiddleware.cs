using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartMenuOptim.API.Middleware
{
    /// <summary>
    /// RateLimittitngMiddleware
    /// 
    /// Description:
    /// This middleware is used to restrict the number of requests a single client IP can make within a specified time window. It helps protect your API from abuse, denial-of-service attacks, and excessive resource consumption by enforcing a configurable rate limit per client.
    /// 
    /// Why use this middleware?
    /// - Prevents clients from overwhelming your API with too many requests in a short period.
    /// - Improves API reliability and fairness by ensuring all clients have equal access.
    /// - Centralizes rate limiting logic, making it easy to maintain and update.
    /// 
    /// Where to use:
    /// - Register this middleware early in the ASP.NET Core pipeline (in Program.cs or Startup.cs) to ensure all requests are rate-limited before reaching your controllers or services.
    /// 
    /// How to use:
    /// - Add to the middleware pipeline with: app.UseMiddleware<RateLimittitngMiddleware>();
    /// - Configure the rate limit and time window as needed in the middleware code.
    /// 
    /// The middleware uses a thread-safe ConcurrentDictionary to track request counts and timestamps per client IP.
    /// </summary>
    public class RateLimittitngMiddleware
    {
        private readonly RequestDelegate _next;
        // Thread-safe dictionary to store request counts and timestamps per client IP
        private static ConcurrentDictionary<string, (DateTime Timestamp, int Count)> _requestCounts = new();
        private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);
        private const int MaxRequestsPerWindow = 60; // Max 60 requests per minute
        private readonly ILogger<RateLimittitngMiddleware> _logger;
        public RateLimittitngMiddleware(RequestDelegate next, ILogger<RateLimittitngMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            // Get the client's IP address
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            // Atomically add or update the request count and timestamp for this IP
            _requestCounts.AddOrUpdate(clientIp,
                // If this is the first request from this IP, initialize with current time and count 1
                addValueFactory: _ => (now, 1),
                // If entry exists, update it based on time window and count
                updateValueFactory: (_, entry) =>
                {
                    // Check if the current request is within the allowed time window
                    if (now - entry.Timestamp < TimeWindow)
                    {
                        // If the request count exceeds the limit, block the request
                        if (entry.Count >= MaxRequestsPerWindow)
                        {
                            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                            context.Response.WriteAsync("Too many requests. Please try again later.").Wait();
                            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                            // Return the same entry to avoid incrementing further
                            return entry;
                        }
                        // Otherwise, increment the request count
                        return (entry.Timestamp, entry.Count + 1);
                    }
                    else
                    {
                        // If the time window has passed, reset the count and timestamp
                        return (now, 1);
                    }
                });

            // Double-check if the request should be blocked (for async context safety)
            if (_requestCounts.TryGetValue(clientIp, out var updatedEntry))
            {
                // If still within the time window and count exceeds the limit, do not proceed
                if (now - updatedEntry.Timestamp < TimeWindow && updatedEntry.Count > MaxRequestsPerWindow)
                {
                    // Already handled above, but this ensures no further processing
                    return;
                }
            }
            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
