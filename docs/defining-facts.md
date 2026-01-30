# Defining Facts

This document explains what a "fact" is in LightRules, how facts are stored and accessed, and how you typically use them when writing rules. It is written for beginners and assumes no prior experience with rule engines.

## What is a fact?

A fact is a piece of information known by the rule engine at runtime. It is a named value that represents some piece of data about the current situation or state the rules should reason about. Examples of facts: "temperature = 18", "customer = \"Alice\"", or "isWeekend = true".

In LightRules a fact is represented by two classes:

- `Fact` (non-generic): stores the name and the value as an `object?`.
- `Fact<T>` (generic): a type-safe wrapper around `Fact` that exposes the value as `T`.

Facts are identified by their name. Names must be unique inside a `Facts` collection: if you add a fact with an existing name, it replaces the previous fact.

## The `Facts` collection

`Facts` is the collection type used by the rules engine to store and pass facts around. It behaves like a small, named dictionary of facts with convenience helpers for typed access.

Key behavior and API (summary):

- Uniqueness: fact names are unique inside a `Facts` instance. Adding/setting a fact with an existing name replaces the previous value.
- Indexer: `facts["name"]` gets or sets a fact value as `object?`. Setting the indexer to `null` removes the fact.
- Set: `Set<T>(string name, T value)` adds or replaces a `Fact<T>` (throws if `value` is null).
- Add: `Add(Fact)` / `Add<T>(Fact<T>)` add facts; existing facts with the same name are replaced.
- Get<T>: `Get<T>(string name)` returns `default(T)` if the fact does not exist, or casts and returns the value if present.
- TryGetValue<T>: `TryGetValue<T>(string name, out T? value)` returns `true` only when the fact exists and its runtime value is assignable to `T`.
- GetFact / TryGetFact: access the raw `Fact` object stored in the collection.
- TryGetFact<T>: attempt to retrieve a `Fact<T>`; if the stored fact is non-generic but its value is assignable to `T`, a typed copy is returned.
- ToDictionary: returns a shallow copy of facts as a `Dictionary<string, object?>`.
- Enumeration: `Facts` implements `IEnumerable<Fact>` so you can iterate over stored facts.
- Clear / Remove: methods to remove facts.

## Why both `Fact` and `Fact<T>`?

- The non-generic `Fact` provides a uniform container the `Facts` collection can hold without losing the actual runtime value. It avoids the need to keep separate typed collections for different value types.
- `Fact<T>` provides compile-time type safety when you know the value type. The collection stores both kinds; helper methods (`TryGetFact<T>`, `TryGetValue<T>`) bridge between them.

Having both types makes the API flexible: rules can work with strongly-typed facts when appropriate, and the engine/framework can still store arbitrary values.

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
A: `Get<T>` attempts a cast and will throw or produce an invalid cast if the stored runtime value is not assignable to `T`. Use `TryGetValue<T>` to safely test and retrieve typed values.

## Where to look next

- Check the `docs/defining-rules.md` document for how rules are written and how conditions and actions bind to facts.
