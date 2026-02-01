namespace LightRules.Core.Fluent;

using System.Collections.Generic;

/// <summary>
/// Helpers to create and compose <see cref="IAction"/> instances in a fluent manner.
/// </summary>
public static class ActionBuilder
{
    public static IAction From(Func<Facts, Facts> func) => Actions.From(func ?? throw new ArgumentNullException(nameof(func)));

    public static IAction From(Action<Facts> action) => Actions.From(action ?? throw new ArgumentNullException(nameof(action)));

    /// <summary>
    /// Compose multiple actions into a single IAction executed in order.
    /// </summary>
    public static IAction Compose(IEnumerable<IAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var list = new List<IAction>(actions);
        return Actions.From(facts =>
        {
            return list.Aggregate(facts, (current1, a) => a.Execute(current1));
        });
    }
}
