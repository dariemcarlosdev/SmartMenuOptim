/*
 * File: ComponentStateBase.cs
 * Base class for component state containers
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides a base implementation for state container pattern.
 * Manages common state properties (loading, error, data) and notifications.
 * 
 * Design Patterns:
 * - State Container Pattern: Centralized state management for components
 * - Observer Pattern: StateHasChanged notification mechanism
 */

namespace SmartMenuOptim.Server.State;

/// <summary>
/// Base class for component state containers providing common state management.
/// </summary>
/// <typeparam name="TData">The type of data managed by this state container.</typeparam>
/// <remarks>
/// <para><strong>Usage:</strong></para>
/// <para>Inherit from this class to create specific state containers for your components.
/// Register as Scoped service for per-circuit state in Blazor Server.</para>
/// </remarks>
public abstract class ComponentStateBase<TData> where TData : class
{
    private TData? _data;
    private bool _isLoading;
    private string? _error;

    /// <summary>
    /// Event raised when state changes. Components should subscribe to this
    /// and call StateHasChanged().
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// The data managed by this state container.
    /// </summary>
    public TData? Data
    {
        get => _data;
        protected set
        {
            _data = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Indicates whether a loading operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        protected set
        {
            _isLoading = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// The current error message, if any.
    /// </summary>
    public string? Error
    {
        get => _error;
        protected set
        {
            _error = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Indicates whether data has been loaded successfully.
    /// </summary>
    public bool HasData => Data is not null;

    /// <summary>
    /// Indicates whether an error has occurred.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    /// <summary>
    /// Resets the state to initial values.
    /// </summary>
    public virtual void Reset()
    {
        _data = null;
        _isLoading = false;
        _error = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the state to loading mode.
    /// </summary>
    protected void SetLoading()
    {
        _isLoading = true;
        _error = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the state with loaded data.
    /// </summary>
    /// <param name="data">The loaded data.</param>
    protected void SetData(TData data)
    {
        _data = data;
        _isLoading = false;
        _error = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the state with an error.
    /// </summary>
    /// <param name="error">The error message.</param>
    protected void SetError(string error)
    {
        _error = error;
        _isLoading = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Notifies subscribers that state has changed.
    /// </summary>
    protected void NotifyStateChanged() => OnStateChanged?.Invoke();
}
