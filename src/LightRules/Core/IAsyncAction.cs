namespace LightRules.Core;

/// <summary>
/// Represents an asynchronous action to execute when a rule's condition evaluates to true.
/// </summary>
public interface IAsyncAction
{
    /// <summary>
    /// Execute the action asynchronously when the rule's condition evaluates to true.
    /// Returns the Facts instance that should be used for subsequent rules.
    /// </summary>
    /// <param name="facts">Facts known at the time of execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Helper factory for creating <see cref="IAsyncAction"/> instances.
/// </summary>
public static class AsyncActions
{
    /// <summary>
    /// Create an <see cref="IAsyncAction"/> from an async function.
    /// </summary>
    public static IAsyncAction From(Func<Facts, CancellationToken, Task<Facts>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return new DelegateAsyncAction(func);
    }

    /// <summary>
    /// Create an <see cref="IAsyncAction"/> from an async function (without cancellation token).
    /// </summary>
    public static IAsyncAction From(Func<Facts, Task<Facts>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return new DelegateAsyncAction((facts, _) => func(facts));
    }

    /// <summary>
    /// Wrap a synchronous <see cref="IAction"/> as an <see cref="IAsyncAction"/>.
    /// </summary>
    public static IAsyncAction FromSync(IAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new SyncActionWrapper(action);
    }

    /// <summary>
    /// Wrap a synchronous function as an <see cref="IAsyncAction"/>.
    /// </summary>
    public static IAsyncAction FromSync(Func<Facts, Facts> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return new SyncActionWrapper(Actions.From(func));
    }

    private sealed class DelegateAsyncAction(Func<Facts, CancellationToken, Task<Facts>> func) : IAsyncAction
    {
        public Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default)
            => func(facts, cancellationToken);
    }

    private sealed class SyncActionWrapper(IAction action) : IAsyncAction
    {
        public Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(action.Execute(facts));
        }
    }
}
