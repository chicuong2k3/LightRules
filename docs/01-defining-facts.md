# Defining Facts

<!-- Table of contents -->
- [What is a fact?](#what-is-a-fact)
- [The Facts collection](#the-facts-collection)
- [Thread safety](#thread-safety)
- [Using facts inside conditions and actions](#using-facts-inside-conditions-and-actions)
- [Examples](#examples)
- [FAQ](#faq)

## What is a fact?

A fact is a piece of information known by the rule engine at runtime. It is a named value that represents some piece of data about the current situation or state the rules should reason about. Examples of facts: "temperature = 18", "customer = \"Alice\"", or "isWeekend = true".

In LightRules a fact is represented by two classes:

| Class | Description |
|-------|-------------|
| `Fact` | Non-generic class that stores the name and the value as `object?`. |
| `Fact<T>` | Type-safe wrapper around `Fact` that exposes the value as `T`. |

Facts are identified by their name. Names must be unique inside a `Facts` collection: if you add a fact with an existing name, it replaces the previous fact.

## The Facts collection

`Facts` is an immutable, thread-safe collection used by the rules engine to store and pass facts around. It behaves like a small, named dictionary of facts with convenience helpers for typed access.

Key characteristics:

- **Immutable**: All mutation methods (`Set`, `Add`, `Remove`) return a new `Facts` instance.
- **Thread-safe**: Uses `ImmutableDictionary` internally, safe for concurrent access.
- **Case-sensitive**: Fact names use ordinal (case-sensitive) comparison.

## Thread safety

`Facts` is fully thread-safe:

```csharp
// Safe to share across threads
var facts = new Facts().AddOrReplaceFact("counter", 0);

// Each thread gets its own copy when mutating
var thread1Facts = facts.AddOrReplaceFact("counter", 1);
var thread2Facts = facts.AddOrReplaceFact("counter", 2);

// Original facts is unchanged
if (facts.TryGetFactValue<int>("counter", out var original))
{
    Console.WriteLine(original); // 0
}
```

## Using facts inside conditions and actions

When you write condition and action methods (either directly on `IRule` implementations or as methods discovered by attributes), facts are how data flows into those methods.

- Accept a `Facts` parameter to get all known facts.
- Use `[Fact("name")]` attribute on parameters for automatic binding from `Facts`.

Example:

```csharp
[Rule("WeatherRule")]
public class WeatherRule
{
    [Condition]
    public bool ItRains([Fact("rain")] bool rain) => rain;

    [Action]
    public Facts TakeAnUmbrella(Facts facts)
    {
        Console.WriteLine("It rains, take an umbrella!");
        return facts.AddOrReplaceFact("umbrellaTaken", true);
    }
}
```

## Examples

```csharp
// Facts is immutable - all mutation methods return a new instance
var facts = new Facts();

// Add typed fact (returns new Facts instance)
facts = facts.AddOrReplaceFact("quantity", 10);

// Add using Fact instance
facts = facts.AddFact(new Fact("customer", "Alice"));

if (facts.TryGetFactValue<int>("quantity", out var qty))
{
    Console.WriteLine($"Quantity: {qty}");
}

// Check if fact exists
if (facts.ContainsFact("customer"))
{
    Console.WriteLine("Customer fact exists");
}

// Get fact count
Console.WriteLine($"Total facts: {facts.Count}");

// Try get raw fact
if (facts.TryGetFact("customer", out var rawFact))
{
    Console.WriteLine(rawFact.Value); // prints "Alice"
}

// Try get typed fact
if (facts.TryGetFact<string>("customer", out var typedFact))
{
    Console.WriteLine(typedFact.Value); // prints "Alice"
}

// Remove (returns new Facts instance)
facts = facts.RemoveFact("customer");

// Get as dictionary (for serialization/logging)
var dict = facts.ToDictionary();
```

## FAQ

**Q: What should I store as facts?**

Store any data that rules need to evaluate conditions or perform actions. Keep facts small and focused (e.g., sensor readings, request headers, computation results). Avoid storing large blobs or unrelated state.

**Q: Can two facts have the same name?**

No. Within a single `Facts` instance, names are unique. Adding a fact with an existing name replaces the previous fact.

**Q: What happens if I try to get a fact with the wrong type?**

Use `TryGetFactValue<T>` to safely test and retrieve typed values. If the stored runtime value is not assignable to `T`,
it returns `false` and sets `value` to `default(T)`. This is the only safe way to read facts.

**Q: Is the fact name comparison case-sensitive?**

Yes, `Facts` uses an ordinal (case-sensitive) comparison when matching names. Be consistent with your fact naming.

**Q: How do I get the final facts after running rules?**

The engine's `Fire` method returns the final `Facts` instance after all rules have executed:

```csharp
var finalFacts = engine.Fire(rules, facts);
```

**Q: Is Facts thread-safe?**

Yes. `Facts` uses `ImmutableDictionary` internally and is fully thread-safe for concurrent reads. Since it's immutable, each mutation returns a new instance, so there are no race conditions.
