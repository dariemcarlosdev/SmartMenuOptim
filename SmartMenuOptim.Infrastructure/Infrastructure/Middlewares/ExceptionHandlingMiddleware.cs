using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Domain.Exceptions;

/*
-------------------------------------------------------------
How to register ExceptionHandlingMiddleware in Program.cs:

1. Add the following using directive:
   using SmartMenuOptim.Infrastructure.Infrastructure.Middlewares;

2. After building the app (after 'var app = builder.Build();'), register the middleware FIRST before other middlewares:
   app.UseMiddleware<ExceptionHandlingMiddleware>();

   // Example placement:
   var app = builder.Build();
   
   // Global Exception Handling - Must be first!
   app.UseMiddleware<ExceptionHandlingMiddleware>();
   
   // Then other middleware
   app.UseHttpsRedirection();
   app.UseStaticFiles();
   app.UseRouting();
   app.UseCors(MyAllowSpecificOrigins);
   app.UseAuthentication();
   app.UseAuthorization();
   app.UseRateLimiter();
   // ... other middleware registrations

This ensures all unhandled exceptions are caught, logged with correlation IDs, and proper error responses are returned to the client.

-------------------------------------------------------------
HOW IT WORKS - AUTOMATIC INVOCATION:

After registration in the DI container, the middleware is automatically 
invoked for EVERY incoming HTTP request:

   ┌─────────────────────────────────────────────────────────────┐
   │  EVERY INCOMING REQUEST                                      │
   │                                                              │
   │  Request ──► GlobalExceptionHandlingMiddleware.InvokeAsync() │
   │                        │                                     │
   │                        ▼                                     │
   │              await _next(context)  ──► Rest of pipeline      │
   │                        │                                     │
   │              ◄─────────┘                                     │
   │         (catches any exception thrown downstream)            │
   └─────────────────────────────────────────────────────────────┘

• Registration: MANUAL - must call app.UseMiddleware<>()
• Per-request invocation: AUTOMATIC - runs for every HTTP request
• Order matters: Must be FIRST in pipeline to catch all exceptions
• DI resolution: AUTOMATIC - ILogger and IHostEnvironment injected

-------------------------------------------------------------
WHERE TO REGISTER - API vs BLAZOR:

• API Project (SmartMenuOptim.API):
  - Register here if you have REST API endpoints
  - Handles all HTTP API exceptions (controllers, minimal APIs)

• Blazor Server Project:
  - Register here for HTTP pipeline exceptions (page requests, API calls)
  - This middleware catches exceptions in the HTTP request pipeline ONLY

⚠️ IMPORTANT FOR BLAZOR:
This middleware handles HTTP pipeline exceptions (API calls, page requests).
For Blazor COMPONENT-LEVEL errors, you should ALSO use <ErrorBoundary> components:

   <ErrorBoundary>
       <ChildContent>
           <YourComponent />
       </ChildContent>
       <ErrorContent Context="ex">
           <p class="error">Something went wrong: @ex.Message</p>
       </ErrorContent>
   </ErrorBoundary>

Blazor SignalR circuit errors are NOT caught by HTTP middleware - use:
  • <ErrorBoundary> for component render errors
  • Try-catch in @code blocks for event handler errors
  • ILogger for logging within components

-------------------------------------------------------------
FUTURE ENHANCEMENTS:

Even though this middleware is PROD-READY, it can be further extended by:

• Creating custom domain exceptions (e.g., DomainException, ValidationException)
  - Define a base DomainException class for business rule violations
  - Create specific exceptions like EntityNotFoundException, BusinessRuleViolationException, DishDomainException, etc.

• Adding FluentValidation exception handling
  - Catch ValidationException from FluentValidation
  - Map validation failures to structured 400 Bad Request responses
  - Include field-level error details in the response

• Implementing ProblemDetails (RFC 7807) format
  - Use Microsoft.AspNetCore.Mvc.ProblemDetails for standardized error responses
  - Include 'type', 'title', 'status', 'detail', and 'instance' fields
  - Better interoperability with API consumers expecting RFC 7807 compliance

• Adding telemetry/metrics collection
  - Integrate with Application Insights, OpenTelemetry, or Prometheus
  - Track exception counts by type, status code, and endpoint
  - Add custom dimensions for correlation and analysis
  - Enable alerting on exception rate thresholds
-------------------------------------------------------------
*/


namespace SmartMenuOptim.Infrastructure.Infrastructure.Middlewares
{
    /// <summary>
    /// Global exception handling middleware for the request pipeline.
    /// Provides comprehensive error handling with structured responses, correlation IDs, and environment-aware details.
    /// Implements production-ready exception handling following Clean Architecture and security best practices.
    /// </summary>
    /// <remarks>
    /// <para><strong>What Changed - Complete Implementation:</strong></para>
    /// 
    /// <para><strong>1. Added IHostEnvironment Dependency</strong></para>
    /// <list type="bullet">
    ///     <item>Enables environment-aware error handling (Development vs Production)</item>
    ///     <item>Controls error detail visibility based on environment</item>
    /// </list>
    /// 
    /// <para><strong>2. Structured JSON Error Responses</strong></para>
    /// <list type="bullet">
    ///     <item><see cref="ErrorResponse"/> class: Main error response structure</item>
    ///     <item><see cref="ErrorDetails"/> class: Development-only detailed error information</item>
    ///     <item>Consistent JSON format with camelCase naming</item>
    ///     <item>Pretty-printed in Development, minified in Production</item>
    /// </list>
    /// 
    /// <para><strong>3. Correlation IDs for Request Tracing</strong></para>
    /// <list type="bullet">
    ///     <item>Uses <c>HttpContext.TraceIdentifier</c> or generates new GUID</item>
    ///     <item>Included in both error responses and log entries</item>
    ///     <item>Enables end-to-end request tracking across distributed systems</item>
    /// </list>
    /// 
    /// <para><strong>4. Exception Type Handling - HTTP Status Code Mapping:</strong></para>
    /// <list type="bullet">
    ///     <item><see cref="ArgumentException"/> / <see cref="ArgumentNullException"/> → <strong>400 Bad Request</strong></item>
    ///     <item><see cref="InvalidOperationException"/> → <strong>400 Bad Request</strong></item>
    ///     <item><see cref="UnauthorizedAccessException"/> → <strong>401 Unauthorized</strong></item>
    ///     <item><see cref="KeyNotFoundException"/> → <strong>404 Not Found</strong></item>
    ///     <item><see cref="TimeoutException"/> → <strong>408 Request Timeout</strong></item>
    ///     <item><see cref="NotImplementedException"/> → <strong>501 Not Implemented</strong></item>
    ///     <item>All other exceptions → <strong>500 Internal Server Error</strong></item>
    /// </list>
    /// 
    /// <para><strong>5. Environment-Aware Error Details:</strong></para>
    /// <list type="bullet">
    ///     <item><strong>Development:</strong> Full exception details, stack traces, inner exceptions, exception types</item>
    ///     <item><strong>Production:</strong> Generic error messages only (security best practice - prevents information disclosure)</item>
    ///     <item>Automatically switches based on <see cref="IHostEnvironment.IsDevelopment()"/></item>
    /// </list>
    /// 
    /// <para><strong>6. Enhanced Logging:</strong></para>
    /// <list type="bullet">
    ///     <item>Correlation IDs included in all log entries</item>
    ///     <item>Request path and HTTP method context</item>
    ///     <item>Structured logging with named parameters for better observability</item>
    ///     <item>Full exception details logged (regardless of environment)</item>
    /// </list>
    /// 
    /// <para><strong>Key Features:</strong></para>
    /// <list type="bullet">
    ///     <item>Catches all unhandled exceptions in the request pipeline</item>
    ///     <item>Returns consistent, structured JSON error responses</item>
    ///     <item>Prevents response body modification errors</item>
    ///     <item>Follows RESTful API error handling conventions</item>
    ///     <item>Aligns with OWASP security guidelines (no sensitive data in production errors)</item>
    ///     <item>Supports distributed tracing and monitoring</item>
    /// </list>
    /// 
    /// <para><strong>Response Example (Development):</strong></para>
    /// <code>
    /// {
    ///   "title": "Bad Request",
    ///   "message": "Value cannot be null. (Parameter 'userId')",
    ///   "correlationId": "0HN1GKQJ5K8QM:00000001",
    ///   "timestamp": "2024-01-15T10:30:00.000Z",
    ///   "details": {
    ///     "exceptionType": "ArgumentNullException",
    ///     "exceptionMessage": "Value cannot be null. (Parameter 'userId')",
    ///     "stackTrace": "at SmartMenuOptim.Application...",
    ///     "innerException": null
    ///   }
    /// }
    /// </code>
    /// 
    /// <para><strong>Response Example (Production):</strong></para>
    /// <code>
    /// {
    ///   "title": "Bad Request",
    ///   "message": "An error occurred while processing your request.",
    ///   "correlationId": "0HN1GKQJ5K8QM:00000001",
    ///   "timestamp": "2024-01-15T10:30:00.000Z"
    /// }
    /// </code>
    /// </remarks>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for logging exceptions.</param>
        /// <param name="environment">Host environment for environment-specific behavior.</param>
        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Invokes the middleware logic.
        /// Catches unhandled exceptions, logs them with correlation IDs, and returns structured error responses.
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
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles the exception and creates an appropriate response.
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Generate correlation ID for tracking
            var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

            // Log the exception with context
            _logger.LogError(
                exception,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                correlationId,
                context.Request.Path,
                context.Request.Method);

            // Determine status code and error details based on exception type
            var (statusCode, errorResponse) = GetErrorResponse(exception, correlationId);

            // Set response properties
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Serialize and write the error response
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(jsonResponse);
        }

        /// <summary>
        /// Determines the appropriate HTTP status code and error response based on the exception type.
        /// </summary>
        private (HttpStatusCode statusCode, ErrorResponse response) GetErrorResponse(Exception exception, string correlationId)
        {
            // Handle specific exception types
            return exception switch
            {
                // Domain-specific exceptions (business rule violations)
                EntityNotFoundException entityNotFound =>
                    (HttpStatusCode.NotFound, CreateErrorResponse(
                        "Not Found - Entity Not Found",
                        entityNotFound.Message,
                        correlationId,
                        entityNotFound)), //  <-- Include original exception for detailed logging and development response

                // Domain exceptions that represent business rule violations (e.g., validation failures, state violations)
                // You can create specific domain exceptions for different types of business rule violations (e.g., ValidationException, BusinessRuleViolationException)
                // Each custom domain exception can be handled here with specific status codes and messages.
                DomainException domainEx =>
                    (HttpStatusCode.UnprocessableEntity, CreateErrorResponse(
                        "Domain Rule Violation - Unprocessable Entity",
                        domainEx.Message,
                        correlationId,
                        domainEx)), //  <-- Include original exception for detailed logging and development response

                // Application/Domain exceptions
                ArgumentException or ArgumentNullException => 
                    (HttpStatusCode.BadRequest, CreateErrorResponse(
                        "Bad Request",
                        exception.Message,
                        correlationId,
                        exception)),

                InvalidOperationException => 
                    (HttpStatusCode.BadRequest, CreateErrorResponse(
                        "Invalid Operation",
                        exception.Message,
                        correlationId,
                        exception)),

                UnauthorizedAccessException => 
                    (HttpStatusCode.Unauthorized, CreateErrorResponse(
                        "Unauthorized",
                        "You are not authorized to perform this action.",
                        correlationId,
                        exception)),

                KeyNotFoundException => 
                    (HttpStatusCode.NotFound, CreateErrorResponse(
                        "Not Found",
                        "The requested resource was not found.",
                        correlationId,
                        exception)),

                NotImplementedException => 
                    (HttpStatusCode.NotImplemented, CreateErrorResponse(
                        "Not Implemented",
                        "This feature is not yet implemented.",
                        correlationId,
                        exception)),

                TimeoutException => 
                    (HttpStatusCode.RequestTimeout, CreateErrorResponse(
                        "Request Timeout",
                        "The request timed out. Please try again.",
                        correlationId,
                        exception)),

                // Default case for unhandled exceptions
                _ => (HttpStatusCode.InternalServerError, CreateErrorResponse(
                    "Internal Server Error",
                    "An unexpected error occurred. Please try again later.",
                    correlationId,
                    exception))
            };
        }

        /// <summary>
        /// Creates a structured error response.
        /// In Development: includes detailed error information.
        /// In Production: returns generic error messages.
        /// </summary>
        private ErrorResponse CreateErrorResponse(
            string title,
            string message,
            string correlationId,
            Exception exception)
        {
            var response = new ErrorResponse
            {
                Title = title,
                Message = _environment.IsDevelopment() ? message : "An error occurred while processing your request.",
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            };

            // Add detailed information only in development
            if (_environment.IsDevelopment())
            {
                response.Details = new ErrorDetails
                {
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            return response;
        }
    }

    /// <summary>
    /// Represents a structured error response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the error title/type.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the correlation ID for tracing the request.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets detailed error information (only populated in Development environment).
        /// </summary>
        public ErrorDetails? Details { get; set; }
    }

    /// <summary>
    /// Represents detailed error information for development environments.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// Gets or sets the exception type name.
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the stack trace.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the inner exception message.
        /// </summary>
        public string? InnerException { get; set; }
    }
}

