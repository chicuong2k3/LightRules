namespace LightRules.Core;

/// <summary>
/// Represents a condition that can be evaluated against a set of facts.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Evaluate the condition according to the known facts.
    /// </summary>
    /// <param name="facts">Known facts available when evaluating the rule.</param>
    /// <returns>True if the condition is satisfied; otherwise false.</returns>
    bool Evaluate(Facts facts);
}

/// <summary>
/// Helper factory and common instances for <see cref="ICondition"/>.
/// </summary>
public static class Conditions
{
    /// <summary>
    /// A no-op condition that always returns false.
    /// </summary>
    public static readonly ICondition False = new AlwaysFalseCondition();

    /// <summary>
    /// A no-op condition that always returns true.
    /// </summary>
    public static readonly ICondition True = new AlwaysTrueCondition();

    /// <summary>
    /// Create an <see cref="ICondition"/> from a <see cref="Func{Facts,Boolean}"/> delegate.
    /// </summary>
    /// <param name="func">A function that evaluates the facts.</param>
    /// <returns>An <see cref="ICondition"/> that wraps the provided function.</returns>
    public static ICondition From(Func<Facts, bool> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        return new FuncCondition(func);
    }

    private sealed class AlwaysFalseCondition : ICondition
    {
        public bool Evaluate(Facts facts) => false;
    }

    private sealed class AlwaysTrueCondition : ICondition
    {
        public bool Evaluate(Facts facts) => true;
    }

    private sealed class FuncCondition(Func<Facts, bool> func) : ICondition
    {
        private readonly Func<Facts, bool> _func = func ?? throw new ArgumentNullException(nameof(func));

        public bool Evaluate(Facts facts) => _func(facts);
    }
}