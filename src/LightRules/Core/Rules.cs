using System.Collections;

namespace LightRules.Core;

/// <summary>
/// Thread-safe ordered collection of rules. Rules are ordered by priority (ascending)
/// and then by name (ordinal, case-insensitive). Rule names are unique within the
/// collection; registering a rule with an existing name replaces the previous rule.
/// </summary>
public class Rules : IEnumerable<IRule>
{
    // Comparer that orders by Priority then Name (case-insensitive)
    private static readonly IComparer<IRule> RuleComparer = Comparer<IRule>.Create((a, b) =>
    {
        var cmp = a.Priority.CompareTo(b.Priority);
        return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    });

    private readonly SortedSet<IRule> _rules;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

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
    /// Existing rules with the same name are replaced. Thread-safe.
    /// </summary>
    /// <param name="rules">Rules to register.</param>
    public void Register(params IRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _lock.EnterWriteLock();
        try
        {
            foreach (var rule in rules)
            {
                if (rule == null) throw new ArgumentNullException(nameof(rules), "One of the provided rules is null");

                // Remove any existing rule with the same name (case-insensitive) before adding
                var existing = FindRuleByNameInternal(rule.Name);
                if (existing != null)
                {
                    _rules.Remove(existing);
                }

                _rules.Add(rule);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Unregister one or more rules. Thread-safe.
    /// </summary>
    /// <param name="rules">Rules to unregister.</param>
    public void Unregister(params IRule[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _lock.EnterWriteLock();
        try
        {
            foreach (var r in rules)
            {
                if (r == null) throw new ArgumentNullException(nameof(rules), "One of the provided rules is null");
                _rules.Remove(r);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Unregister a rule by name (case-insensitive). Thread-safe.
    /// </summary>
    /// <param name="ruleName">Name of the rule to unregister.</param>
    public void Unregister(string ruleName)
    {
        if (string.IsNullOrWhiteSpace(ruleName)) throw new ArgumentNullException(nameof(ruleName));
        _lock.EnterWriteLock();
        try
        {
            var rule = FindRuleByNameInternal(ruleName);
            if (rule != null)
            {
                _rules.Remove(rule);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns true if the rule set is empty.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _rules.Count == 0;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Clear all registered rules. Thread-safe.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _rules.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Number of registered rules.
    /// </summary>
    public int Size
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _rules.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Returns an enumerator over a snapshot of the currently registered rules in sorted order.
    /// Thread-safe - creates a copy for enumeration.
    /// </summary>
    public IEnumerator<IRule> GetEnumerator()
    {
        IRule[] snapshot;
        _lock.EnterReadLock();
        try
        {
            snapshot = _rules.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }
        return ((IEnumerable<IRule>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Find a rule by name (case-insensitive). Thread-safe.
    /// </summary>
    public IRule? FindRuleByName(string ruleName)
    {
        _lock.EnterReadLock();
        try
        {
            return FindRuleByNameInternal(ruleName);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Internal method that assumes lock is already held
    private IRule? FindRuleByNameInternal(string ruleName)
    {
        foreach (var r in _rules)
        {
            if (string.Equals(r.Name, ruleName, StringComparison.OrdinalIgnoreCase)) return r;
        }
        return null;
    }
}
