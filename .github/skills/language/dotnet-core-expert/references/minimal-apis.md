# Minimal APIs Reference

> **Load when:** Creating endpoints, configuring routing, adding middleware in ASP.NET Core minimal APIs.

## Endpoint Architecture

```
Program.cs → MapGroup → MapGet/Post/Put/Delete → Handler Logic
                │
                ├── .RequireAuthorization()
                ├── .WithTags()
                ├── .Produces<T>()
                └── .AddEndpointFilter<T>()
```

## Typed Endpoint Pattern

Organize endpoints in dedicated classes rather than cramming everything into `Program.cs`.

```csharp
// Presentation/Endpoints/EscrowEndpoints.cs
public static class EscrowEndpoints
{
    public static void MapEscrowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/escrows")
            .WithTags("Escrows")
            .RequireAuthorization("EscrowOperator");

        group.MapGet("/", GetAllEscrows)
            .WithName("GetAllEscrows")
            .Produces<PaginatedList<EscrowSummaryDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetEscrowById)
            .WithName("GetEscrowById")
            .Produces<EscrowDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateEscrow)
            .WithName("CreateEscrow")
            .Produces<CreateEscrowResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}/release", ReleaseEscrow)
            .WithName("ReleaseEscrow")
            .RequireAuthorization("EscrowManager")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> GetAllEscrows(
        [AsParameters] GetEscrowsQuery query,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetEscrowById(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEscrowByIdQuery(id), ct);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    private static async Task<IResult> CreateEscrow(
        CreateEscrowCommand command, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return TypedResults.Created($"/api/v1/escrows/{result.EscrowId}", result);
    }

    private static async Task<IResult> ReleaseEscrow(
        Guid id, IMediator mediator, CancellationToken ct)
    {
        await mediator.Send(new ReleaseEscrowCommand(id), ct);
        return TypedResults.NoContent();
    }
}
```

## Registration in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map all endpoint groups
app.MapEscrowEndpoints();
app.MapPaymentEndpoints();
app.MapDisputeEndpoints();

app.Run();
```

## Endpoint Filters (Middleware at Endpoint Level)

```csharp
public sealed class ValidationFilter<TRequest>(
    IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null) return await next(context);

        var result = await validator.ValidateAsync(request);
        return result.IsValid
            ? await next(context)
            : TypedResults.ValidationProblem(result.ToDictionary());
    }
}
```

## Route Groups and Versioning

```csharp
// API versioning with route groups
var v1 = app.MapGroup("/api/v1").RequireAuthorization();
var v2 = app.MapGroup("/api/v2").RequireAuthorization();

v1.MapEscrowEndpoints();  // /api/v1/escrows/...
v2.MapEscrowEndpointsV2(); // /api/v2/escrows/... (new version)
```

## Parameter Binding

```csharp
// Bind from route, query, header, body automatically
group.MapGet("/{id:guid}", (Guid id) => ...);                    // Route
group.MapGet("/", ([FromQuery] int page, [FromQuery] int size) => ...);  // Query string
group.MapPost("/", (CreateEscrowCommand body) => ...);            // JSON body
group.MapGet("/", ([FromHeader(Name = "X-Correlation-Id")] string correlationId) => ...);

// Complex parameter binding with [AsParameters]
public sealed record GetEscrowsQuery(
    [FromQuery] int Page = 1,
    [FromQuery] int PageSize = 20,
    [FromQuery] string? Status = null) : IRequest<PaginatedList<EscrowSummaryDto>>;
```

## OpenAPI / Swagger Configuration

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "NexTruzt Escrow API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
```
