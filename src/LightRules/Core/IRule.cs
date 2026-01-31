namespace LightRules.Core;

/// <summary>
/// Represents a business rule. Implementations expose a name, description and priority
/// and provide methods to evaluate the rule against a set of facts and to execute actions.
/// </summary>
public interface IRule : IComparable<IRule>
{
    /// <summary>
    /// Default name used when no explicit name is provided.
    /// </summary>
    public const string DefaultName = "rule";

    /// <summary>
    /// Default description used when no explicit description is provided.
    /// </summary>
    public const string DefaultDescription = "";

    /// <summary>
    /// Default priority used when no explicit priority is provided.
    /// </summary>
    public const int DefaultPriority = int.MaxValue - 1;

    /// <summary>
    /// The rule name which must be unique within a rules registry.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The rule description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The rule priority. Lower values indicate higher priority when comparing rules.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Evaluate the rule against the provided facts. Implementations should return
    /// <c>true</c> when the rule's conditions are satisfied given the current facts.
    /// </summary>
    /// <param name="facts">Facts known when evaluating the rule.</param>
    bool Evaluate(Facts facts);

    /// <summary>
    /// Execute the actions associated with this rule using the provided facts and return
    /// the resulting Facts instance to be used by subsequent rules.
    /// </summary>
    /// <param name="facts">Facts known at the time of execution.</param>
    Facts Execute(Facts facts);
}