namespace LightRules.Core.Fluent;

using System.Collections.Generic;

/// <summary>
/// Fluent builder for constructing <see cref="IRule"/> instances (DefaultRule).
/// </summary>
public sealed class RuleBuilder
{
    private string _name = IRule.DefaultName;
    private string? _description = IRule.DefaultDescription;
    private int _priority = IRule.DefaultPriority;
    private ICondition _condition = Conditions.False;
    private readonly List<IAction> _actions = new();

    private RuleBuilder() { }

    /// <summary>
    /// Start a new rule builder. Optionally provide a name.
    /// </summary>
    public static RuleBuilder Create(string? name = null)
    {
        var rb = new RuleBuilder();
        if (!string.IsNullOrEmpty(name)) rb._name = name;
        return rb;
    }

    public RuleBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    public RuleBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public RuleBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }

    public RuleBuilder When(ICondition condition)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        return this;
    }

    public RuleBuilder When(Func<Facts, bool> predicate)
    {
        _condition = Conditions.From(predicate ?? throw new ArgumentNullException(nameof(predicate)));
        return this;
    }

    public RuleBuilder Then(IAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
        return this;
    }

    public RuleBuilder Then(Func<Facts, Facts> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        _actions.Add(Actions.From(func));
        return this;
    }

    public RuleBuilder Then(Action<Facts> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(Actions.From(action));
        return this;
    }

    public RuleBuilder ThenMany(IEnumerable<IAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _actions.AddRange(actions);
        return this;
    }

    public RuleBuilder ClearActions()
    {
        _actions.Clear();
        return this;
    }

    /// <summary>
    /// Build the <see cref="IRule"/> instance (DefaultRule) from the configured values.
    /// </summary>
    public IRule Build()
    {
        return new DefaultRule(_name, _description, _priority, _condition, _actions);
    }
}
