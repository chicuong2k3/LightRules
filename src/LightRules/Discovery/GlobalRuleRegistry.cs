using System.Collections.Concurrent;

namespace LightRules.Discovery;

/// <summary>
/// Thread-safe global registry for rule metadata. Rules are automatically registered
/// via ModuleInitializer when their assembly loads - no reflection required.
/// </summary>
public static class GlobalRuleRegistry
{
    private static readonly ConcurrentBag<RuleMetadata> _rules = new();

    /// <summary>
    /// Register rule metadata. Called automatically by generated ModuleInitializers.
    /// </summary>
    public static void Register(params RuleMetadata[] rules)
    {
        foreach (var rule in rules)
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Register rule metadata from an enumerable source.
    /// </summary>
    public static void Register(IEnumerable<RuleMetadata> rules)
    {
        foreach (var rule in rules)
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Get all registered rule metadata, ordered by priority.
    /// </summary>
    public static IEnumerable<RuleMetadata> GetAll()
    {
        return _rules.OrderBy(m => m.Priority).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get the count of registered rules.
    /// </summary>
    public static int Count => _rules.Count;

    /// <summary>
    /// Clear all registered rules. Useful for testing.
    /// </summary>
    public static void Clear()
    {
        while (_rules.TryTake(out _)) { }
    }
}
