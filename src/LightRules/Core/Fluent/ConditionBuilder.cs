namespace LightRules.Core.Fluent;

/// <summary>
/// Small fluent helpers to compose <see cref="ICondition"/> instances.
/// </summary>
public static class ConditionBuilder
{
    public static ICondition From(Func<Facts, bool> predicate) => Conditions.From(predicate ?? throw new ArgumentNullException(nameof(predicate)));

    public static ICondition True() => Conditions.True;

    public static ICondition False() => Conditions.False;

    public static ICondition And(ICondition a, ICondition b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return new AndCondition(a, b);
    }

    public static ICondition Or(ICondition a, ICondition b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return new OrCondition(a, b);
    }

    public static ICondition Not(ICondition a)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        return new NotCondition(a);
    }
}

internal sealed class AndCondition(ICondition a, ICondition b) : ICondition
{
    public bool Evaluate(Facts facts) => a.Evaluate(facts) && b.Evaluate(facts);
}

internal sealed class OrCondition(ICondition a, ICondition b) : ICondition
{
    public bool Evaluate(Facts facts) => a.Evaluate(facts) || b.Evaluate(facts);
}

internal sealed class NotCondition(ICondition a) : ICondition
{
    public bool Evaluate(Facts facts) => !a.Evaluate(facts);
}
