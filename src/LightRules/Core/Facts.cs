using System.Collections;
using System.Collections.Immutable;

namespace LightRules.Core;

/// <summary>
/// Thread-safe immutable collection of named facts. All mutation methods return a new Facts instance.
/// </summary>
public sealed class Facts : IEnumerable<Fact>
{
    private readonly ImmutableDictionary<string, object?> _map;

    public Facts()
    {
        _map = ImmutableDictionary<string, object?>.Empty.WithComparers(StringComparer.Ordinal);
    }

    private Facts(ImmutableDictionary<string, object?> map)
    {
        _map = map;
    }

    /// <summary>
    /// Gets the runtime value of a fact by name (or null if not present).
    /// </summary>
    public object? this[string name] => Get<object>(name);

    /// <summary>
    /// Return a new Facts instance with the given typed fact set (add or replace).
    /// </summary>
    public Facts Set<T>(string name, T value)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));
        return new Facts(_map.SetItem(name, value));
    }

    /// <summary>
    /// Add a <c>Fact&lt;T&gt;</c> instance; returns a new Facts instance.
    /// </summary>
    public Facts Add<T>(Fact<T> fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return new Facts(_map.SetItem(fact.Name, fact.Value));
    }

    /// <summary>
    /// Add a non-generic Fact instance; returns a new Facts instance.
    /// </summary>
    public Facts Add(Fact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return new Facts(_map.SetItem(fact.Name, fact.Value));
    }

    /// <summary>
    /// Remove a fact by name and return a new Facts instance.
    /// </summary>
    public Facts Remove(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        if (!_map.ContainsKey(factName)) return this;
        return new Facts(_map.Remove(factName));
    }

    /// <summary>
    /// Remove a fact instance and return a new Facts instance.
    /// </summary>
    public Facts Remove(Fact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return Remove(fact.Name);
    }

    /// <summary>
    /// Remove a typed fact instance and return a new Facts instance.
    /// </summary>
    public Facts Remove<T>(Fact<T> fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return Remove(fact.Name);
    }

    /// <summary>
    /// Get the value of a fact by its name. Returns default(T) if not present.
    /// </summary>
    public T? Get<T>(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        if (_map.TryGetValue(factName, out var v))
        {
            return (T)v!;
        }
        return default;
    }

    /// <summary>
    /// Try get the value of a fact by its name. Returns true if found and assignable to T.
    /// </summary>
    public bool TryGetValue<T>(string factName, out T? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(factName)) return false;
        if (!_map.TryGetValue(factName, out var v) || v == null) return false;
        if (v is T t)
        {
            value = t;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get a fact by name.
    /// </summary>
    public Fact? GetFact(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        if (_map.TryGetValue(factName, out var v))
        {
            return new Fact(factName, v);
        }
        return null;
    }

    /// <summary>
    /// Try get a fact by name.
    /// </summary>
    public bool TryGetFact(string factName, out Fact? fact)
    {
        fact = null;
        if (string.IsNullOrWhiteSpace(factName)) return false;
        if (_map.TryGetValue(factName, out var v))
        {
            fact = new Fact(factName, v);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Try get a typed <c>Fact&lt;T&gt;</c> from the collection.
    /// </summary>
    public bool TryGetFact<T>(string factName, out Fact<T>? fact)
    {
        fact = null;
        if (string.IsNullOrWhiteSpace(factName)) return false;
        if (!_map.TryGetValue(factName, out var v) || v == null) return false;
        if (v is T t)
        {
            fact = new Fact<T>(factName, t);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Return a copy of the facts as a dictionary.
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>(_map, StringComparer.Ordinal);
    }

    /// <summary>
    /// Number of facts in the collection.
    /// </summary>
    public int Count => _map.Count;

    /// <summary>
    /// Check if a fact with the given name exists.
    /// </summary>
    public bool ContainsKey(string factName) => _map.ContainsKey(factName);

    /// <summary>
    /// Create a shallow copy of the Facts collection. Since Facts is immutable, returns the same instance.
    /// </summary>
    public Facts Clone() => this;

    /// <summary>
    /// Return an enumerator on the set of facts.
    /// </summary>
    public IEnumerator<Fact> GetEnumerator()
    {
        foreach (var kv in _map)
        {
            yield return new Fact(kv.Key, kv.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return "[" + string.Join(",", _map.Select(kv => kv.Key + "=" + kv.Value)) + "]";
    }
}
