/*
 * File: ResultExtensions.cs
 * Extension methods for Result pattern operations
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides extension methods for mapping, chaining, and transforming Results.
 */

using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Application.Common;

/// <summary>
/// Extension methods for Result pattern operations.
/// </summary>
public static class ResultExtensions
{
    // ═══════════════════════════════════════════════════════════════════════
    // MAPPING EXTENSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a successful result to a new value.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        return result.IsSuccess 
            ? Result.Success(mapper(result.Value)) 
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Maps a successful result to a new result asynchronously.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result, 
        Func<TIn, Task<TOut>> mapper)
    {
        return result.IsSuccess 
            ? Result.Success(await mapper(result.Value)) 
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Chains a successful result to another operation.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result, 
        Func<TIn, Result<TOut>> next)
    {
        return result.IsSuccess 
            ? next(result.Value) 
            : Result.Failure<TOut>(result.Error);
    }

    /// <summary>
    /// Chains a successful result to another async operation.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result, 
        Func<TIn, Task<Result<TOut>>> next)
    {
        return result.IsSuccess 
            ? await next(result.Value) 
            : Result.Failure<TOut>(result.Error);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MATCH EXTENSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Pattern matches on the result to execute the appropriate function.
    /// </summary>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<string, TOut> onFailure)
    {
        return result.IsSuccess 
            ? onSuccess(result.Value) 
            : onFailure(result.Error);
    }

    /// <summary>
    /// Pattern matches on the result to execute the appropriate async function.
    /// </summary>
    public static async Task<TOut> MatchAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> onSuccess,
        Func<string, Task<TOut>> onFailure)
    {
        return result.IsSuccess 
            ? await onSuccess(result.Value) 
            : await onFailure(result.Error);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DOMAIN RESULT CONVERSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts a DomainResult to an Application Result.
    /// </summary>
    public static Result ToApplicationResult(this DomainResult domainResult)
    {
        return domainResult.IsSuccess 
            ? Result.Success() 
            : Result.Failure(domainResult.Error!.Message);
    }

    /// <summary>
    /// Converts a DomainResult&lt;T&gt; to an Application Result&lt;T&gt;.
    /// </summary>
    public static Result<T> ToApplicationResult<T>(this DomainResult<T> domainResult)
    {
        return domainResult.IsSuccess 
            ? Result.Success(domainResult.Value) 
            : Result.Failure<T>(domainResult.Error!.Message);
    }

    /// <summary>
    /// Converts a DomainResult&lt;T&gt; to an Application Result&lt;TOut&gt; with mapping.
    /// </summary>
    public static Result<TOut> ToApplicationResult<TIn, TOut>(
        this DomainResult<TIn> domainResult, 
        Func<TIn, TOut> mapper)
    {
        return domainResult.IsSuccess 
            ? Result.Success(mapper(domainResult.Value)) 
            : Result.Failure<TOut>(domainResult.Error!.Message);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TAP EXTENSIONS (Side Effects)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// Executes an async action if the result is successful.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public static Result<T> TapError<T>(this Result<T> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error);
        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ENSURE EXTENSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures a condition is met on the value, or returns a failure.
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result, 
        Func<T, bool> predicate, 
        string errorMessage)
    {
        if (result.IsFailure)
            return result;

        return predicate(result.Value) 
            ? result 
            : Result.Failure<T>(errorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMBINE EXTENSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Combines multiple results into a single result.
    /// Returns failure if any result failed.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToList();
        return failures.Count != 0
            ? Result.Failure(string.Join("; ", failures.Select(f => f.Error)))
            : Result.Success();
    }

    /// <summary>
    /// Gets the value or a default if the result failed.
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, T defaultValue = default!)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }
}
