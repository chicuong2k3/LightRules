namespace LightRules.Core;

/// <summary>
/// Represents an action to execute when a rule's condition evaluates to true.
/// Actions now follow a functional style and return the (possibly) new Facts instance.
/// </summary>
public interface IAction
{
    /// <summary>
    /// Execute the action when the rule's condition evaluates to true.
    /// The method returns the Facts instance that should be used for subsequent rules.
    /// </summary>
    /// <param name="facts">Facts known at the time of execution of the action.</param>
    Facts Execute(Facts facts);
}

/// <summary>
/// Helper factory and common helpers for <see cref="IAction"/> instances.
/// </summary>
public static class Actions
{
    /// <summary>
    /// Create an <see cref="IAction"/> from a function that maps Facts -> Facts.
    /// </summary>
    public static IAction From(Func<Facts, Facts> func)
    {
        return func == null ? throw new ArgumentNullException(nameof(func)) : new DelegateActionFunc(func);
    }

    /// <summary>
    /// Compatibility helper: create an IAction from an Action&lt;Facts&gt; (legacy).
    /// The action will be executed on a cloned Facts instance and the resulting instance will be returned.
    /// </summary>
    public static IAction From(Action<Facts> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new DelegateAction(action);
    }

    private sealed class DelegateActionFunc(Func<Facts, Facts> func) : IAction
    {
        public Facts Execute(Facts facts) => func(facts);
    }

    private sealed class DelegateAction(Action<Facts> action) : IAction
    {
        public Facts Execute(Facts facts)
        {
            action(facts);
            return facts;
        }
    }
}