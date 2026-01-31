using LightRules.Core;

namespace LightRules.Discovery;

/// <summary>
/// Metadata about a discovered rule, including a factory to create instances without reflection.
/// </summary>
public sealed class RuleMetadata
{
    /// <summary>
    /// The adapter type that implements IRule.
    /// </summary>
    public Type RuleType { get; }

    /// <summary>
    /// The rule name from the [Rule] attribute.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The rule description from the [Rule] attribute.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// The rule priority.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Whether the rule is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Tags associated with the rule.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Factory function to create an IRule instance without reflection.
    /// </summary>
    public Func<IRule> Factory { get; }

    /// <summary>
    /// Create RuleMetadata with a factory function (preferred - no reflection).
    /// </summary>
    public RuleMetadata(Type ruleType, string name, string? description, int priority, bool enabled, IReadOnlyList<string> tags, Func<IRule> factory)
    {
        RuleType = ruleType ?? throw new ArgumentNullException(nameof(ruleType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Priority = priority;
        Enabled = enabled;
        Tags = tags ?? Array.Empty<string>();
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Create an IRule instance using the factory. No reflection required.
    /// </summary>
    public IRule CreateInstance() => Factory();
}
