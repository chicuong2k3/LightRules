using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LightRules.Core;

public class Facts : IEnumerable<Fact>
{
    // Fact equality is based on name.
    private readonly HashSet<Fact> _facts = new();

    /// <summary>
    /// Indexer for convenience: get/set fact values by name. Setting to null removes the fact.
    /// </summary>
    public object? this[string name]
    {
        get => Get<object>(name);
        set
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (value == null)
            {
                Remove(name);
                return;
            }
            // Add as a non-generic Fact to preserve the runtime type
            var existing = GetFact(name);
            if (existing != null)
            {
                Remove(existing);
            }
            _facts.Add(new Fact(name, value));
        }
    }

    /// <summary>
    /// Set a fact (add or replace) with the given name and value.
    /// </summary>
    public void Set<T>(string name, T value)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));
        var retrievedFact = GetFact(name);
        if (retrievedFact != null)
        {
            Remove(retrievedFact);
        }
        Add(new Fact<T>(name, value));
    }

    /// <summary>
    /// Add a generic fact, replacing any fact with the same name.
    /// </summary>
    public void Add<T>(Fact<T> fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
        var retrievedFact = GetFact(fact.Name);
        if (retrievedFact != null)
        {
            Remove(retrievedFact);
        }
        _facts.Add(fact);
    }

    /// <summary>
    /// Add a non-generic fact, replacing any fact with the same name.
    /// </summary>
    public void Add(Fact fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
        var retrievedFact = GetFact(fact.Name);
        if (retrievedFact != null)
        {
            Remove(retrievedFact);
        }
        _facts.Add(fact);
    }

    /// <summary>
    /// Remove a fact by name.
    /// </summary>
    public void Remove(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        var fact = GetFact(factName);
        if (fact != null)
        {
            Remove(fact);
        }
    }

    /// <summary>
    /// Remove a fact.
    /// </summary>
    public void Remove(Fact fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
        _facts.Remove(fact);
    }

    /// <summary>
    /// Remove a fact.
    /// </summary>
    public void Remove<T>(Fact<T> fact)
    {
        if (fact == null) throw new ArgumentNullException(nameof(fact));
        _facts.Remove(fact);
    }

    /// <summary>
    /// Get the value of a fact by its name. Returns default(T) if not present.
    /// </summary>
    public T? Get<T>(string factName)
    {
        if (string.IsNullOrWhiteSpace(factName)) throw new ArgumentNullException(nameof(factName));
        var fact = GetFact(factName);
        if (fact != null)
        {
            return (T)fact.Value!;
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
        var fact = GetFact(factName);
        if (fact == null || fact.Value == null) return false;
        if (fact.Value is T t)
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
        return _facts.FirstOrDefault(f => string.Equals(f.Name, factName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Try get a fact by name.
    /// </summary>
    public bool TryGetFact(string factName, out Fact? fact)
    {
        fact = null;
        if (string.IsNullOrWhiteSpace(factName)) return false;
        fact = GetFact(factName);
        return fact != null;
    }

    /// <summary>
    /// Try get a typed Fact<T> from the collection. If the stored fact is a Fact<T> it is returned;
    /// if the stored fact is non-generic but its value is assignable to T, a new Fact<T> copy is returned.
    /// </summary>
    public bool TryGetFact<T>(string factName, out Fact<T>? fact)
    {
        fact = null;
        if (string.IsNullOrWhiteSpace(factName)) return false;
        var f = GetFact(factName);
        if (f == null) return false;
        if (f is Fact<T> typed) 
        {
            fact = typed;
            return true;
        }
        if (f.Value is T t)
        {
            fact = new Fact<T>(f.Name, t);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Return a copy of the facts as a dictionary.
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        var map = new Dictionary<string, object?>();
        foreach (var f in _facts)
        {
            map[f.Name] = f.Value;
        }
        return map;
    }

    /// <summary>
    /// Return an enumerator on the set of facts.
    /// </summary>
    public IEnumerator<Fact> GetEnumerator() => _facts.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Clear facts.
    /// </summary>
    public void Clear() => _facts.Clear();

    public override string ToString()
    {
        return "[" + string.Join(",", _facts.Select(f => f.ToString())) + "]";
    }
}