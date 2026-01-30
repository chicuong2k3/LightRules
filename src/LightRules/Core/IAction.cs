namespace LightRules.Core;

/// <summary>
/// Represents an action to execute when a rule's condition evaluates to true.
/// </summary>
public interface IAction
{
    /// <summary>
    /// Execute the action when the rule's condition evaluates to true.
    /// </summary>
    /// <param name="facts">Facts known at the time of execution of the action.</param>
    void Execute(Facts facts);
}

/// <summary>
/// Helper factory and common helpers for <see cref="IAction"/> instances.
/// </summary>
public static class Actions
{
    /// <summary>
    /// Create an <see cref="IAction"/> from a delegate.
    /// </summary>
    /// <param name="action">The delegate to wrap. Must not be null.</param>
    /// <returns>An <see cref="IAction"/> that invokes the provided delegate.</returns>
    public static IAction From(Action<Facts> action)
    {
        return action == null ? throw new ArgumentNullException(nameof(action)) 
            : new DelegateAction(action);
    }

    private sealed class DelegateAction(Action<Facts> action) : IAction
    {
        public void Execute(Facts facts) => action(facts);
    }
}