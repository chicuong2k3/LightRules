# Defining Facts

- [What is a fact?](#what-is-a-fact)
- [The `Facts` collection](#the-facts-collection)
- [Using facts inside conditions and actions](#using-facts-inside-conditions-and-actions)
- [Examples (common operations)](#examples-common-operations)
- [FAQ](#frequently-asked-questions-beginner-friendly)

This document explains what a "fact" is in LightRules, how facts are stored and accessed, and how you typically use them when writing rules. It is written for beginners and assumes no prior experience with rule engines.

## What is a fact?

A fact is a piece of information known by the rule engine at runtime. It is a named value that represents some piece of data about the current situation or state the rules should reason about. Examples of facts: "temperature = 18", "customer = \"Alice\"", or "isWeekend = true".

In LightRules a fact is represented by two classes:

- `Fact` (non-generic): stores the name and the value as an `object?`.
- `Fact<T>` (generic): a type-safe wrapper around `Fact` that exposes the value as `T`.

Facts are identified by their name. Names must be unique inside a `Facts` collection: if you add a fact with an existing name, it replaces the previous fact.

## The `Facts` collection

`Facts` is the collection type used by the rules engine to store and pass facts around. It behaves like a small, named dictionary of facts with convenience helpers for typed access.

For a concise API reference (methods, behaviors and quick notes) see the dedicated API page: `docs/10-facts-api.md`.

## Using facts inside conditions and actions

When you write condition and action methods (either directly on `IRule` implementations or as methods discovered by attributes), facts are how data flows into those methods.

- You can accept a `Facts` parameter to get all known facts.
- You can accept typed parameters and use a `[Fact("name")]` attribute on the parameter (if using attribute-based binding) so the engine will bind the fact value to the parameter before calling your method.

Example rule class (simplified):

```csharp
[Rule("WeatherRule")]
public class WeatherRule : IRule
{
    [Condition]
    public bool ItRains([Fact("rain")] bool rain)
    {
        return rain;
    }

    [Action]
    public void TakeAnUmbrella(Facts facts)
    {
        Console.WriteLine("It rains, take an umbrella!");
        // You can add or update facts:
        facts.Set("umbrellaTaken", true);
    }

    // IRule members (can delegate to the attribute-backed methods):
    public string Name => "WeatherRule";
    public string Description => "Take an umbrella when it rains";
    public int Priority => 0;

    public bool Evaluate(Facts facts)
    {
        return facts.TryGetValue<bool>("rain", out var r) && ItRains(r);
    }

    public void Execute(Facts facts) => TakeAnUmbrella(facts);
}
```

Notes:
- Mutating `Facts` inside an action (for example, adding or updating a fact) affects the same `Facts` instance seen by subsequent rules, unless the engine explicitly snapshots facts before evaluation. Check the engine execution semantics if you need isolation.
- If a fact is missing, typed access methods either return `default(T)` (`Get<T>`) or `false` (`TryGetValue<T>`). Always use the `Try*` patterns when you are not sure a fact exists or when the type might not match.

## Examples (common operations)

```csharp
var facts = new Facts();

// Add typed fact
facts.Set("quantity", 10);

// Add using indexer (stores as non-generic Fact)
facts["customer"] = "Alice";

// Read typed value (throws if the stored value cannot be cast to int)
int q = facts.Get<int>("quantity");

// Safe read
if (facts.TryGetValue<int>("quantity", out var safeQ))
{
    Console.WriteLine(safeQ);
}

// Try get raw fact
if (facts.TryGetFact("customer", out var rawFact))
{
    Console.WriteLine(rawFact.Value); // prints "Alice"
}

// Try get typed fact even if stored as non-generic
if (facts.TryGetFact<string>("customer", out var typedFact))
{
    Console.WriteLine(typedFact.Value); // prints "Alice"
}

// Remove
facts.Remove("customer");

// Clear all
facts.Clear();
```

## Frequently asked questions (beginner-friendly)

Q: "What should I store as facts?"
A: Store any data that rules need to evaluate conditions or perform actions. Keep facts small and focused (e.g., sensor readings, request headers, computation results). Avoid storing large blobs or unrelated state.

Q: "Can two facts have the same name?"
A: No. Within a single `Facts` instance, names are unique. Adding a fact with an existing name replaces the previous fact.

Q: "What happens if I try to get a fact with the wrong type?"
A: `Get<T>` attempts a cast and will throw an exception if the stored runtime value is not assignable to `T`. Use `TryGetValue<T>` to safely test and retrieve typed values.

Q: "Is the fact name comparison case-sensitive?"
A: Yes, `Facts` uses an ordinal (case-sensitive) comparison when matching names. Be consistent with your fact naming.
