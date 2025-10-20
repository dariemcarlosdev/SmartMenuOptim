using Microsoft.AspNetCore.Http;  
using Microsoft.Extensions.Logging;

/*
-------------------------------------------------------------
How to register ExceptionHandlingMiddleware in Program.cs:

1. Add the following using directive if needed:
   using SmartMenuOptim.Infrastructure.NewFolder;

2. After building the app (after 'var app = builder.Build();'), register the middleware before other middlewares:
   app.UseMiddleware<ExceptionHandlingMiddleware>();

   // Example placement:
   var app = builder.Build();
   app.UseMiddleware<ExceptionHandlingMiddleware>();
   app.UseRateLimiter();
   app.UseCors(MyAllowSpecificOrigins);
   // ... other middleware registrations

This ensures all unhandled exceptions are logged and a generic error response is returned to the client.
-------------------------------------------------------------
*/


namespace SmartMenuOptim.Infrastructure.Middlewares
{
    /// <summary>
    /// Middleware for handling unhandled exceptions in the request pipeline.
    /// Logs the exception and returns a generic error response.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for logging exceptions.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware logic.
        /// Catches unhandled exceptions, logs them, and returns a 500 error response.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception with error severity
                _logger.LogError(ex, "Unhandled exception occurred.");

                // Set the response status code to 500 (Internal Server Error)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                // Write a generic error message to the response. This can be improved to return a JSON response if needed.
                await context.Response.WriteAsync("An unexpected error occurred.");
            }
        }
    }
}

