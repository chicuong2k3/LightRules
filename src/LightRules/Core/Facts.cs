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
    public object? this[string name] => GetFactValue<object>(name);

    /// <summary>
    /// Return a new Facts instance with the given typed fact added or replaced.
    /// </summary>
    public Facts AddOrReplaceFact<T>(string name, T value)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));
        return new Facts(_map.SetItem(name, value));
    }

    /// <summary>
    /// Add a typed Fact instance; returns a new Facts instance.
    /// If a fact with the same name exists, it will be replaced.
    /// </summary>
    public Facts AddFact<T>(Fact<T> fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return new Facts(_map.SetItem(fact.Name, fact.Value));
    }

    /// <summary>
    /// Add a Fact instance; returns a new Facts instance.
    /// If a fact with the same name exists, it will be replaced.
    /// </summary>
    public Facts AddFact(Fact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return new Facts(_map.SetItem(fact.Name, fact.Value));
    }

    /// <summary>
    /// Remove a fact by name and return a new Facts instance.
    /// </summary>
    public Facts RemoveFact(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        if (!_map.ContainsKey(factName)) return this;
        return new Facts(_map.Remove(factName));
    }

    /// <summary>
    /// Remove a fact instance and return a new Facts instance.
    /// </summary>
    public Facts RemoveFact(Fact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return RemoveFact(fact.Name);
    }

    /// <summary>
    /// Remove a typed fact instance and return a new Facts instance.
    /// </summary>
    public Facts RemoveFact<T>(Fact<T> fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        return RemoveFact(fact.Name);
    }

    /// <summary>
    /// Get the value of a fact by its name. Returns default(T) if not present.
    /// Use <see cref="TryGetFactValue{T}"/> for safe access without exceptions on type mismatch.
    /// </summary>
    public T? GetFactValue<T>(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        if (_map.TryGetValue(factName, out var v))
        {
            return (T)v!;
        }
        return default;
    }

    /// <summary>
    /// Try to get the value of a fact by its name. Returns true if found and assignable to T.
    /// </summary>
    public bool TryGetFactValue<T>(string factName, out T? value)
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
    /// Get a Fact object by name. Returns null if not present.
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
    /// Try to get a Fact object by name.
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
    /// Try to get a typed Fact object from the collection.
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
    /// Check if a fact with the given name exists.
    /// </summary>
    public bool ContainsFact(string factName) => _map.ContainsKey(factName);

    /// <summary>
    /// Number of facts in the collection.
    /// </summary>
    public int Count => _map.Count;

    /// <summary>
    /// Return a copy of the facts as a dictionary.
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>(_map, StringComparer.Ordinal);
    }

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
