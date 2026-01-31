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

Note: The engine evaluates conditions against a snapshot of the `Facts` instance (created with `Facts.Clone()`). This means condition code receives an isolated shallow copy and cannot mutate the original facts that actions will observe. Use actions to update facts when you want mutations to be seen by subsequent rules.

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
    public bool ItRains([Fact("rain")] bool rain) => rain;

    [Action]
    public Facts TakeAnUmbrella(Facts facts)
    {
        Console.WriteLine("It rains, take an umbrella!");
        // Return a new Facts instance with the updated value (immutable API)
        return facts.Set("umbrellaTaken", true);
    }

    // IRule members (can delegate to the attribute-backed methods):
    public string Name => "WeatherRule";
    public string Description => "Take an umbrella when it rains";
    public int Priority => 0;

    public bool Evaluate(Facts facts) => facts.TryGetValue<bool>("rain", out var r) && ItRains(r);

    public Facts Execute(Facts facts) => TakeAnUmbrella(facts);
}
```

Notes and best practices for actions and facts

- Keep actions small and single-responsibility: write composable actions that do one thing well (e.g., set a fact, call an external service). Small actions are easier to test and reason about.

- Be explicit about side effects: document what external systems (APIs, databases, message buses) an action touches and which fact keys it modifies. Avoid hidden global side effects.

- Prefer idempotence for external work: when actions call external systems, design them so repeated execution does not produce duplicate or incorrect effects (for example, use idempotency keys, upserts, or check-before-write patterns).

- Error handling: handle expected exceptions inside the action where reasonable. Use structured logging to capture context (rule name, facts snapshot, correlation id). For transient failures, prefer retry/backoff outside the rule engine or via a dedicated retry wrapper; avoid blocking the engine thread with long retry loops unless the engine is designed for it.

- Mutating facts with the immutable API: `Facts` is immutable — actions should return a new `Facts` instance representing the updated state. Do not attempt to mutate the `facts` parameter in-place. Example:

  ```csharp
  // Correct (functional)
  public Facts MarkProcessed(Facts facts)
  {
      return facts.Set("processed", true);
  }
  ```

  If you still have legacy mutation-style code (void methods that modify `Facts`), the project provides compatibility helpers (for example `Actions.From(Action<Facts>)`) and the source generator supports legacy void action methods. However, for clarity and forward-compatibility prefer the functional form.

- Isolation: the engine evaluates conditions on a snapshot of facts and threads the `Facts` instance returned by actions forward. If you need isolation for specific action-side effects or want to compute changes without affecting subsequent rules, operate on a copy and return the appropriate `Facts` instance.

## Examples (common operations)

```csharp
// Facts is immutable - all mutation methods return a new instance
var facts = new Facts();

// Add typed fact (returns new Facts instance)
facts = facts.Set("quantity", 10);

// Add using Fact instance
facts = facts.Add(new Fact("customer", "Alice"));

// Read typed value (returns default if not present; throws on type mismatch)
int q = facts.Get<int>("quantity");

// Safe read (recommended in conditions)
if (facts.TryGetValue<int>("quantity", out var safeQ))
{
    Console.WriteLine(safeQ);
}

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
facts = facts.Remove("customer");

// Get as dictionary (for serialization/logging)
var dict = facts.ToDictionary();
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

## Migration: moving to immutable Facts

This release changes `Facts` to an immutable, record-like collection. Key migration steps:

- Creating / setting facts
  - Old mutable code:
    ```csharp
    var facts = new Facts();
    facts.Set("quantity", 5);
    facts["customer"] = "Alice";
    ```
  - New immutable code:
    ```csharp
    var facts = new Facts();
    facts = facts.Set("quantity", 5);
    facts = facts.Add(new Fact("customer", "Alice"));
    ```

- Actions and rules
  - `IAction.Execute` and `IRule.Execute` now return a `Facts` instance. Update implementations to return the new facts after applying changes.
  - For POCO attribute-based methods, the source generator supports both returning `Facts` and legacy `void` methods (for backward compatibility). Prefer returning `Facts` where possible.

- Example: converting a legacy action to functional:

Old:
```csharp
public void OnSuccess(Facts facts)
{
    facts.Set("orderAccepted", true);
}
```

New (functional):
```csharp
public Facts OnSuccess(Facts facts)
{
    return facts.Set("orderAccepted", true);
}
```

Compatibility helpers

- `Actions.From(Action<Facts>)` is available during migration: it runs the legacy mutation-style delegate on a temporary mutable builder and returns an immutable `Facts` instance.

Testing & verification

- Run the sample app and your tests to verify behavior. The engine's `Fire` method now returns the final `Facts` instance, which you should capture if you need to inspect results after firing.

```csharp
var finalFacts = engine.Fire(rules, facts);
```
