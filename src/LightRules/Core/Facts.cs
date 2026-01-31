using System.Collections;

namespace LightRules.Core;

/// <summary>
/// Immutable collection of named facts. All mutation-like methods return a new Facts instance.
/// For migration compatibility a temporary <see cref="MutableFacts"/> builder is provided.
/// </summary>
public sealed class Facts : IEnumerable<Fact>
{
    private readonly Dictionary<string, object?> _map;

    public Facts()
    {
        _map = new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private Facts(Dictionary<string, object?> map)
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
        var copy = new Dictionary<string, object?>(_map, StringComparer.Ordinal);
        copy[name] = value;
        return new Facts(copy);
    }

    /// <summary>
    /// Add a <c>Fact&lt;T&gt;</c> instance; returns a new Facts instance.
    /// </summary>
    public Facts Add<T>(Fact<T> fact)
    {
        ArgumentNullException.ThrowIfNull(fact);
        var copy = new Dictionary<string, object?>(_map, StringComparer.Ordinal)
        {
            [fact.Name] = fact.Value
        };
        return new Facts(copy);
    }

    /// <summary>
    /// Add a non-generic Fact instance; returns a new Facts instance.
    /// </summary>
    public Facts Add(Fact fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
        var copy = new Dictionary<string, object?>(_map, StringComparer.Ordinal)
        {
            [fact.Name] = fact.Value
        };
        return new Facts(copy);
    }

    /// <summary>
    /// Remove a fact by name and return a new Facts instance.
    /// </summary>
    public Facts Remove(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        if (!_map.ContainsKey(factName)) return this;
        var copy = new Dictionary<string, object?>(_map, StringComparer.Ordinal);
        copy.Remove(factName);
        return new Facts(copy);
    }

    /// <summary>
    /// Remove a fact instance and return a new Facts instance.
    /// </summary>
    public Facts Remove(Fact fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
        return Remove(fact.Name);
    }

    /// <summary>
    /// Remove a typed fact instance and return a new Facts instance.
    /// </summary>
    public Facts Remove<T>(Fact<T> fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
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
    /// Create a shallow copy of the Facts collection. For immutables this returns the same instance.
    /// </summary>
    public Facts Clone() => this;

    /// <summary>
    /// Obtain a mutable builder for this Facts instance. Useful for compatibility with legacy Action&lt;Facts&gt; delegates.
    /// </summary>
    internal MutableFacts ToMutable()
    {
        return new MutableFacts(new Dictionary<string, object?>(_map, StringComparer.Ordinal));
    }

    /// <summary>
    /// Create an immutable Facts instance from a MutableFacts builder.
    /// </summary>
    internal static Facts FromMutable(MutableFacts m)
    {
        return new Facts(new Dictionary<string, object?>(m.GetInternalMap(), StringComparer.Ordinal));
    }

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

    // Mutable builder used internally for compatibility with legacy mutation-style delegates
    internal sealed class MutableFacts
    {
        private readonly Dictionary<string, object?> _internal;
        public MutableFacts(Dictionary<string, object?> map) { _internal = map; }
        internal Dictionary<string, object?> GetInternalMap() => _internal;

        public object? this[string name]
        {
            get => _internal.TryGetValue(name, out var v) ? v : null;
            set
            {
                if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
                if (value == null) _internal.Remove(name);
                else _internal[name] = value;
            }
        }

        public void Set<T>(string name, T value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));
            _internal[name] = value;
        }

        public void Add<T>(Fact<T> fact)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));
            _internal[fact.Name] = fact.Value;
        }

        public void Add(Fact fact)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));
            _internal[fact.Name] = fact.Value;
        }

        public void Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            _internal.Remove(name);
        }

        public T? Get<T>(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (_internal.TryGetValue(name, out var v)) return (T)v!;
            return default;
        }

        public bool TryGetValue<T>(string name, out T? value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!_internal.TryGetValue(name, out var v) || v == null) return false;
            if (v is T t) { value = t; return true; }
            return false;
        }
    }
}