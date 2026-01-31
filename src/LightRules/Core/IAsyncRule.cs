namespace LightRules.Core;

/// <summary>
/// Represents an asynchronous business rule. Extends <see cref="IRule"/> with async evaluation and execution.
/// </summary>
public interface IAsyncRule : IRule
{
    /// <summary>
    /// Evaluate the rule asynchronously against the provided facts.
    /// </summary>
    /// <param name="facts">Facts known when evaluating the rule.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task<bool> EvaluateAsync(Facts facts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the actions associated with this rule asynchronously.
    /// </summary>
    /// <param name="facts">Facts known at the time of execution.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Adapter to wrap a synchronous <see cref="IRule"/> as an <see cref="IAsyncRule"/>.
/// </summary>
public sealed class AsyncRuleAdapter : IAsyncRule
{
    private readonly IRule _inner;

    public AsyncRuleAdapter(IRule rule)
    {
        _inner = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    public string Name => _inner.Name;
    public string Description => _inner.Description;
    public int Priority => _inner.Priority;

    public bool Evaluate(Facts facts) => _inner.Evaluate(facts);

    public Facts Execute(Facts facts) => _inner.Execute(facts);

    public Task<bool> EvaluateAsync(Facts facts, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_inner.Evaluate(facts));
    }

    public Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_inner.Execute(facts));
    }

    public int CompareTo(IRule? other) => _inner.CompareTo(other);
}

/// <summary>
/// Extension methods for async rule operations.
/// </summary>
public static class AsyncRuleExtensions
{
    /// <summary>
    /// Convert a synchronous rule to an async rule.
    /// If the rule already implements <see cref="IAsyncRule"/>, returns it directly.
    /// </summary>
    public static IAsyncRule AsAsync(this IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule as IAsyncRule ?? new AsyncRuleAdapter(rule);
    }
}
