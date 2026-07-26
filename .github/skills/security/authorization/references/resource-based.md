# Resource-Based Authorization

## Overview

Resource-based authorization checks whether a specific user can perform a specific operation on a specific entity. This is essential for fintech escrow transactions where ownership and role determine access.

## Operation Requirements

```csharp
namespace EscrowApp.Application.Authorization;

public static class Operations
{
    public static readonly OperationAuthorizationRequirement View =
        new() { Name = nameof(View) };
    public static readonly OperationAuthorizationRequirement Create =
        new() { Name = nameof(Create) };
    public static readonly OperationAuthorizationRequirement Release =
        new() { Name = nameof(Release) };
    public static readonly OperationAuthorizationRequirement Dispute =
        new() { Name = nameof(Dispute) };
    public static readonly OperationAuthorizationRequirement Cancel =
        new() { Name = nameof(Cancel) };
    public static readonly OperationAuthorizationRequirement Approve =
        new() { Name = nameof(Approve) };
}
```

## Escrow Transaction Authorization Handler

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

public sealed class EscrowTransactionAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, EscrowTransaction>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        EscrowTransaction transaction)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        var escrowRole = context.User.FindFirst("EscrowRole")?.Value;

        if (userId is null)
            return Task.CompletedTask;

        var isAdmin = escrowRole is "Admin";
        var isAgent = escrowRole is "Agent" or "Admin";
        var isBuyer = transaction.BuyerId == userId;
        var isSeller = transaction.SellerId == userId;
        var isParty = isBuyer || isSeller;

        var authorized = requirement.Name switch
        {
            nameof(Operations.View) => isParty || isAgent,
            nameof(Operations.Release) => isAgent && CanRelease(transaction),
            nameof(Operations.Dispute) => isParty && CanDispute(transaction),
            nameof(Operations.Cancel) => (isBuyer && CanBuyerCancel(transaction))
                                          || isAdmin,
            nameof(Operations.Approve) => isAgent && CanApprove(transaction),
            _ => false
        };

        if (authorized)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static bool CanRelease(EscrowTransaction tx)
        => tx.Status is TransactionStatus.FundsHeld
            or TransactionStatus.DisputeResolved;

    private static bool CanDispute(EscrowTransaction tx)
        => tx.Status is TransactionStatus.FundsHeld;

    private static bool CanBuyerCancel(EscrowTransaction tx)
        => tx.Status is TransactionStatus.Pending;

    private static bool CanApprove(EscrowTransaction tx)
        => tx.Status is TransactionStatus.PendingApproval;
}
```

### Register Handler
```csharp
builder.Services
    .AddScoped<IAuthorizationHandler, EscrowTransactionAuthorizationHandler>();
```

## Using IAuthorizationService in MediatR Handlers

### Command Handler with Resource-Based Check
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;

public sealed class ReleaseEscrowFundsCommand(
    Guid transactionId, ClaimsPrincipal user) : IRequest<Result>
{
    public Guid TransactionId { get; } = transactionId;
    public ClaimsPrincipal User { get; } = user;
}

public sealed class ReleaseEscrowFundsHandler(
    IAuthorizationService authService,
    IEscrowTransactionRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<ReleaseEscrowFundsHandler> logger)
    : IRequestHandler<ReleaseEscrowFundsCommand, Result>
{
    public async Task<Result> Handle(
        ReleaseEscrowFundsCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(
            request.TransactionId, cancellationToken);

        if (transaction is null)
            return Result.NotFound("Escrow transaction not found.");

        // Resource-based authorization check
        var authResult = await authService.AuthorizeAsync(
            request.User, transaction, Operations.Release);

        if (!authResult.Succeeded)
        {
            logger.LogWarning(
                "User {UserId} unauthorized to release transaction {TxId}",
                request.User.FindFirst("sub")?.Value,
                request.TransactionId);
            return Result.Forbidden(
                "Not authorized to release funds on this transaction.");
        }

        transaction.ReleaseFunds();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Funds released for transaction {TxId} by {UserId}",
            request.TransactionId,
            request.User.FindFirst("sub")?.Value);

        return Result.Success();
    }
}
```

### Query Handler with View Authorization
```csharp
public sealed class GetTransactionDetailsHandler(
    IAuthorizationService authService,
    IEscrowTransactionRepository repository)
    : IRequestHandler<GetTransactionDetailsQuery, Result<TransactionDto>>
{
    public async Task<Result<TransactionDto>> Handle(
        GetTransactionDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(
            request.TransactionId, cancellationToken);

        if (transaction is null)
            return Result<TransactionDto>.NotFound();

        var authResult = await authService.AuthorizeAsync(
            request.User, transaction, Operations.View);

        if (!authResult.Succeeded)
            return Result<TransactionDto>.Forbidden();

        return Result<TransactionDto>.Success(
            transaction.ToDto());
    }
}
```

## Ownership Checks Pattern

For simpler ownership validation without the full authorization handler:

```csharp
public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID claim missing.");

    public static string? GetTenantId(this ClaimsPrincipal principal)
        => principal.FindFirst("tenant_id")?.Value;

    public static bool IsOwnerOf(
        this ClaimsPrincipal principal, IOwnedEntity entity)
        => principal.GetUserId() == entity.OwnerId;

    public static bool IsInTenant(
        this ClaimsPrincipal principal, ITenantEntity entity)
        => principal.GetTenantId() == entity.TenantId;
}

public interface IOwnedEntity
{
    string OwnerId { get; }
}

public interface ITenantEntity
{
    string TenantId { get; }
}
```

## Multi-Tenant Resource Authorization

```csharp
public sealed class TenantResourceHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, ITenantEntity>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        ITenantEntity resource)
    {
        var userTenant = context.User.FindFirst("tenant_id")?.Value;

        // Users can only access resources in their own tenant
        if (userTenant is not null && userTenant == resource.TenantId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

### Register
```csharp
builder.Services
    .AddScoped<IAuthorizationHandler, TenantResourceHandler>();
```
