using System.Collections;

namespace LightRules.Core;

/// <summary>
/// Represents an ordered collection of rules. Rules are ordered by priority (ascending)
/// and then by name (ordinal, case-insensitive). Rule names are unique within the
/// collection; registering a rule with an existing name replaces the previous rule.
/// </summary>
public class Rules : IEnumerable<IRule>
{
    // Comparer that orders by Priority then Name (case-insensitive)
    private static readonly IComparer<IRule> RuleComparer = Comparer<IRule>.Create((a, b) =>
    {
        // a and b are non-nullable according to the interface signature; compare priority then name
        var cmp = a.Priority.CompareTo(b.Priority);
        return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    });

    private readonly SortedSet<IRule> _rules;

    /// <summary>
    /// Create an empty <see cref="Rules"/> collection.
    /// </summary>
    public Rules()
    {
        _rules = new SortedSet<IRule>(RuleComparer);
    }

    /// <summary>
    /// Create a new <see cref="Rules"/> collection and register the provided rules.
    /// </summary>
    /// <param name="rules">Rules to register.</param>
    public Rules(IEnumerable<IRule> rules) : this()
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var r in rules)
        {
            Register(r);
        }
    }

    /// <summary>
    /// Create a new <see cref="Rules"/> collection and register the provided rules.
    /// </summary>
    /// <param name="rules">Rules to register.</param>
    public Rules(params IRule[] rules) : this()
    {
        Register(rules);
    }

    /// <summary>
    /// Register one or more rules. Each argument must be an <see cref="IRule"/> instance.
    /// Existing rules with the same name are replaced.
    /// </summary>
    /// <param name="rules">Rules to register.</param>
    public void Register(params IRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var rule in rules)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rules), "One of the provided rules is null");

            // Remove any existing rule with the same name (case-insensitive) before adding
            var existing = FindRuleByName(rule.Name);
            if (existing != null)
            {
                _rules.Remove(existing);
            }

            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Unregister one or more rules. Each argument may be an <see cref="IRule"/> instance
    /// or a <see cref="Type"/> that implements <see cref="IRule"/>.
    /// </summary>
    /// <param name="rules">Objects representing rules to unregister.</param>
    public void Unregister(params IRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var r in rules)
        {
            if (r == null) throw new ArgumentNullException(nameof(rules), "One of the provided rules is null");
            _rules.Remove(r);
        }
    }

    /// <summary>
    /// Unregister a rule by name (case-insensitive).
    /// </summary>
    /// <param name="ruleName">Name of the rule to unregister.</param>
    public void Unregister(string ruleName)
    {
        if (string.IsNullOrWhiteSpace(ruleName)) throw new ArgumentNullException(nameof(ruleName));
        var rule = FindRuleByName(ruleName);
        if (rule != null)
        {
            _rules.Remove(rule);
        }
    }

    /// <summary>
    /// Returns true if the rule set is empty.
    /// </summary>
    public bool IsEmpty => _rules.Count == 0;

    /// <summary>
    /// Clear all registered rules.
    /// </summary>
    public void Clear() => _rules.Clear();

    /// <summary>
    /// Number of registered rules.
    /// </summary>
    public int Size => _rules.Count;

    /// <summary>
    /// Returns an enumerator over the currently registered rules in sorted order.
    /// It is not intended to remove rules using this enumerator.
    /// </summary>
    public IEnumerator<IRule> GetEnumerator() => _rules.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IRule? FindRuleByName(string ruleName)
    {
        foreach (var r in _rules)
        {
            if (string.Equals(r.Name, ruleName, StringComparison.OrdinalIgnoreCase)) return r;
        }
        return null;
    }
}

