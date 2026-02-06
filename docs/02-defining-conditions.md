# Defining Conditions

<!-- Table of contents -->
- [What is a Condition?](#what-is-a-condition)
- [Built-in helpers](#built-in-helpers)
- [Implementing a condition](#implementing-a-condition)
- [Semantics and best practices](#semantics-and-best-practices)
- [Parameter binding with attribute-based rules](#parameter-binding-with-attribute-based-rules)
- [Composition](#composition)
- [Examples](#examples)
- [Troubleshooting/FAQ](#troubleshootingfaq)

## What is a Condition?

A Condition is a predicate that decides whether a rule should fire. 
It inspects the current set of facts and returns either `true` (the condition is satisfied) 
or `false` (it is not).

In LightRules the Condition abstraction is represented by the `ICondition` interface:

```csharp
public interface ICondition
{
    bool Evaluate(Facts facts);
}
```

A condition receives a `Facts` instance and returns a boolean. 
The engine calls `Evaluate` when deciding if the rule associated to this condition 
must be executed.

## Built-in helpers

The `Conditions` static class provides three useful helpers:

- `Conditions.True`: a condition that always returns `true`.
- `Conditions.False`: a condition that always returns `false`.
- `Conditions.From(Func<Facts,bool>)` create an `ICondition` from a delegate.

These are convenient when you need a simple condition or want to provide a lambda 
instead of implementing a class.

Example:

```csharp
// A condition that checks if the "temperature" fact is over 30 degrees
var cond = Conditions.From(facts => facts.TryGetFactValue<int>("temperature", out var t) && t > 30);
if (cond.Evaluate(facts)) // if the condition is met
{
    // do something
}
```

## Implementing a condition

You can implement `ICondition` directly when you need a reusable type:

```csharp
public class IsAdultCondition : ICondition
{
    public bool Evaluate(Facts facts)
    {
        return facts.TryGetFactValue<int>("age", out var age) && age >= 18;
    }
}
```

Or you can use `Conditions.From` with a lambda for concise one-off conditions.

## Semantics and best practices

- Pure predicate: A condition should ideally be a pure predicate. It should only read facts and return a boolean result
  without causing side effects. This makes rule evaluation predictable and easier to test.
- Immutable facts (current design): `Facts` is implemented as an immutable, thread-safe container. All mutation-style
  helpers (for example `AddOrReplaceFact`, `AddFact`, `RemoveFact`) return a new `Facts` instance and do not modify the
  original instance.
  - Because `Facts` is immutable, condition code cannot change the facts instance it receives. If you need to change
    facts, do so in an action by returning a new `Facts` instance.
  - The engine threads facts through rule execution: `IAction.Execute(Facts)` returns the `Facts` instance that will be
    used for subsequent rules. This makes state changes explicit and avoids accidental side-effects during evaluation.
- Read-only intent reinforced: Conditions are evaluated against the provided `Facts` instance and must be treated as
  read-only. This is a stronger guarantee than the informal "prefer not to mutate" wording — the runtime enforces
  immutability.
- Null & missing facts: Use `TryGetFactValue<T>` when a fact might be absent or have a different runtime type. This is
  the only safe way to read facts — it returns false if the fact doesn't exist or the type doesn't match.
- Performance: Keep evaluations cheap — they are executed frequently. Avoid blocking IO or expensive computation in
  `Evaluate`.
- Exceptions: If a condition throws an exception, the engine's behavior depends on its implementation. When possible,
  handle expected error cases inside the condition and return `false`.

### Migration note (breaking change)

If you are migrating from an older mutable `Facts` design, update your actions and conditions as follows:

- Conditions: Remove any code that attempted to modify the passed `Facts` instance. Instead, move that logic into an
  action.
- Actions: Ensure actions return a new `Facts` instance containing any updates you need. Use `AddOrReplaceFact` /
  `AddFact` / `RemoveFact` helpers which produce new `Facts` instances.

Example action that sets a fact:

```csharp
public class SetUmbrellaAction : IAction
{
    public Facts Execute(Facts facts)
    {
        return facts.AddOrReplaceFact("umbrellaTaken", true);
    }
}
```

Compatibility helper note: `Actions.From(Action<Facts>)` remains available as a convenience wrapper for legacy code, but
because `Facts` is immutable it cannot be used to produce updated facts — such wrappers will return the same `Facts`
instance unchanged. Prefer `Actions.From(Func<Facts,Facts>)` when your action needs to return a modified facts set.

## Parameter binding with attribute-based rules

If your rules use attribute-based method discovery and parameter binding, the engine may support binding method parameters from facts using a `[Fact("name")]` attribute (see `docs/defining-facts.md` and `docs/defining-rules.md`). In that case a condition method can be written like:

```csharp
[Condition]
public bool CheckAge([Fact("age")] int age)
{
    return age >= 18;
}
```

The engine is responsible for resolving the `age` parameter from the `Facts` collection before invoking the method. 
When a fact is missing or the type is incompatible, the engine must define a clear policy (throw, skip, supply default). Consult the rule discovery/binding documentation.

## Composition

If you need composed logic (AND/OR/NOT), use the provided combinators in `ConditionCombinators` or implement similar small compositors.

The library includes simple combinators: `ConditionCombinators.And(a,b)`, `ConditionCombinators.Or(a,b)`, and `ConditionCombinators.Not(inner)`.

Quick usage examples:

```csharp
var highTemp = Conditions.From(f => f.TryGetFactValue<int>("temp", out var t) && t >= 35);
var isWeekend = Conditions.From(f => f.TryGetFactValue<bool>("isWeekend", out var w) && w);

// Compose: high temp AND weekend
var hotWeekend = ConditionCombinators.And(highTemp, isWeekend);

// Compose: NOT weekend
var notWeekend = ConditionCombinators.Not(isWeekend);

// Compose: high temp OR weekend
var hotOrWeekend = ConditionCombinators.Or(highTemp, isWeekend);

if (hotWeekend.Evaluate(facts)) { /* ... */ }
```

Use these combinators to keep condition logic modular and testable.


## Examples

1) Simple lambda-based condition:

```csharp
var cond = Conditions.From(f => f.TryGetFactValue<bool>("isWeekend", out var w) && w);
if (cond.Evaluate(facts))
{
    Console.WriteLine("It's weekend!");
}
```

2) Reusable condition type:

```csharp
public sealed class HighTemperatureCondition : ICondition
{
    private readonly int _threshold;
    public HighTemperatureCondition(int threshold) => _threshold = threshold;
    public bool Evaluate(Facts facts) => facts.TryGetFactValue<int>("temp", out var t) && t >= _threshold;
}

// Usage
var c = new HighTemperatureCondition(35);
```

## Troubleshooting/FAQ

Q: What if my condition needs to perform expensive work?
A: Offload expensive or blocking operations outside the rule evaluation (precompute, cache results, or use asynchronous processing in the action). Conditions should be fast.

Q: Can I mutate facts inside a condition?
A: No — `Facts` is immutable in the current design. Condition code receives a `Facts` instance that cannot be altered.
If you need to change facts, do it in an action by returning a new `Facts` instance. Mutating shared state outside of
`Facts` (global state, databases, etc.) is still possible but discouraged inside `Evaluate`.

Q: What if the fact type doesn't match the expected type?
A: Use `TryGetFactValue<T>` to safely test and retrieve a typed value. This is the only way to read facts — it returns
`false` if the type doesn't match or the fact doesn't exist.
