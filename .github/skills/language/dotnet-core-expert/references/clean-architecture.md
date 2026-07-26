# Clean Architecture Reference

> **Load when:** Implementing CQRS/MediatR patterns, structuring layers, or configuring dependency injection.

## Layer Dependency Rules

```
┌───────────────────────────────────────────────┐
│ Presentation (Endpoints, Blazor Components)   │
│   Depends on: Application                     │
├───────────────────────────────────────────────┤
│ Infrastructure (EF Core, External Services)   │
│   Depends on: Application, Domain             │
├───────────────────────────────────────────────┤
│ Application (Commands, Queries, Handlers)     │
│   Depends on: Domain                          │
├───────────────────────────────────────────────┤
│ Domain (Entities, Value Objects, Interfaces)  │
│   Depends on: NOTHING (zero external refs)    │
└───────────────────────────────────────────────┘
```

**Rule:** Dependencies point INWARD only. Domain never references Infrastructure or Presentation.

## CQRS with MediatR

### Command Pipeline

```csharp
// 1. Command (Application/Features/Escrows/Release/)
public sealed record ReleaseEscrowCommand(Guid EscrowId) : IRequest<Result<ReleaseEscrowResult>>;

public sealed record ReleaseEscrowResult(Guid EscrowId, string Status, DateTime ReleasedAt);

// 2. Validator (same folder)
public sealed class ReleaseEscrowValidator : AbstractValidator<ReleaseEscrowCommand>
{
    public ReleaseEscrowValidator()
    {
        RuleFor(x => x.EscrowId).NotEmpty();
    }
}

// 3. Handler (same folder)
public sealed class ReleaseEscrowHandler(
    IEscrowRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<ReleaseEscrowHandler> logger) : IRequestHandler<ReleaseEscrowCommand, Result<ReleaseEscrowResult>>
{
    public async Task<Result<ReleaseEscrowResult>> Handle(
        ReleaseEscrowCommand request, CancellationToken ct)
    {
        var escrow = await repository.GetByIdAsync(new EscrowId(request.EscrowId), ct);
        if (escrow is null)
            return Result<ReleaseEscrowResult>.Failure("Escrow not found");

        var releaseResult = escrow.Release();
        if (releaseResult.IsFailure)
            return Result<ReleaseEscrowResult>.Failure(releaseResult.Error);

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Escrow {EscrowId} released", escrow.Id);

        return Result<ReleaseEscrowResult>.Success(
            new(escrow.Id.Value, escrow.Status.ToString(), DateTime.UtcNow));
    }
}
```

### Query Pipeline

```csharp
// Queries are read-only — use AsNoTracking, projections, no unit of work
public sealed record GetEscrowByIdQuery(Guid EscrowId) : IRequest<EscrowDetailDto?>;

public sealed class GetEscrowByIdHandler(
    EscrowDbContext context) : IRequestHandler<GetEscrowByIdQuery, EscrowDetailDto?>
{
    public async Task<EscrowDetailDto?> Handle(
        GetEscrowByIdQuery request, CancellationToken ct)
    {
        return await context.Escrows
            .AsNoTracking()
            .Where(e => e.Id == new EscrowId(request.EscrowId))
            .Select(e => new EscrowDetailDto(
                e.Id.Value,
                e.BuyerId,
                e.SellerId,
                e.Amount.Value,
                e.Amount.Currency,
                e.Status.ToString(),
                e.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
```

## MediatR Pipeline Behaviors

### Validation Behavior

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Logging Behavior

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);
        var sw = Stopwatch.StartNew();
        var response = await next();
        logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", 
            requestName, sw.ElapsedMilliseconds);
        return response;
    }
}
```

## DI Registration

```csharp
// Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

## Folder Structure

```
src/
├── Domain/
│   ├── Entities/          (Escrow.cs, Payment.cs)
│   ├── ValueObjects/      (Money.cs, EscrowId.cs)
│   ├── Enums/             (EscrowStatus.cs)
│   ├── Events/            (EscrowReleasedEvent.cs)
│   └── Interfaces/        (IEscrowRepository.cs)
├── Application/
│   ├── Common/
│   │   ├── Behaviors/     (ValidationBehavior.cs, LoggingBehavior.cs)
│   │   ├── Interfaces/    (IUnitOfWork.cs)
│   │   └── Models/        (Result.cs, PaginatedList.cs)
│   └── Features/
│       └── Escrows/
│           ├── CreateEscrow/    (Command, Validator, Handler)
│           ├── ReleaseEscrow/   (Command, Validator, Handler)
│           └── GetEscrowById/   (Query, Handler)
├── Infrastructure/
│   ├── Persistence/       (EscrowDbContext.cs, Configurations/)
│   ├── Repositories/      (EscrowRepository.cs)
│   └── Services/          (PaymentGatewayAdapter.cs)
└── Presentation/
    ├── Endpoints/         (EscrowEndpoints.cs)
    └── Components/        (Blazor components)
```
