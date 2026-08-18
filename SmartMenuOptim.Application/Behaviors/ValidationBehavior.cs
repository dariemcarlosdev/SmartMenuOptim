using System.Reflection;
using FluentValidation;
using MediatR;
using SmartMenuOptim.Application.Common;

namespace SmartMenuOptim.Application.Behaviors;

/// <summary>
/// Runs registered FluentValidation validators for <typeparamref name="TRequest"/> before the handler executes.
/// Short-circuits to a failed <typeparamref name="TResponse"/> (via its static <c>Failure(string)</c> factory)
/// instead of invoking the handler when validation fails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join(" ", failures.Select(f => f.ErrorMessage));

        // TResponse is Result or Result<T>; Result<T> hides the base Failure(string) factory with
        // "new", so reflecting on typeof(TResponse) resolves to the correct hidden/derived overload.
        var failureFactory = typeof(TResponse).GetMethod("Failure", BindingFlags.Public | BindingFlags.Static, [typeof(string)])
            ?? throw new InvalidOperationException($"{typeof(TResponse).Name} has no static Failure(string) factory.");

        return (TResponse)failureFactory.Invoke(null, [errorMessage])!;
    }
}
