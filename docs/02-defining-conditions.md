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

- Pure predicate: A condition should ideally be a pure predicate, it should only read facts and return a boolean result without causing side effects. This makes rule evaluation predictable and easier to test.
- Read-only intent: Although the `Facts` object can be mutated, prefer not to mutate it from inside `Evaluate`.

> Engine snapshot behavior: The engine evaluates conditions against a snapshot copy of the provided `Facts` (using `Facts.Clone()`), so condition code cannot mutate the original `Facts` instance that actions will later observe. This makes it safe to assume conditions are read-only and prevents surprising interactions caused by condition-side effects. If you need to update facts, do so in actions.

- Null & missing facts: Use `TryGetFactValue<T>` when a fact might be absent or have a different runtime type. 
`GetFactValue<T>` will cast the stored value and may throw if the runtime type does not match; prefer `Try*` patterns in conditions.
- Performance: Keep evaluations cheap, they are executed frequently. Avoid blocking IO or expensive computation in `Evaluate`.
- Exceptions: If a condition throws an exception, the engine's behavior depends on its implementation. 
When possible, handle expected error cases inside the condition and return `false`.

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

Example implementations (already provided in the runtime):

```csharp
// Equivalent conceptual implementation
public class AndCondition : ICondition
{
    private readonly ICondition _a;
    private readonly ICondition _b;
    public AndCondition(ICondition a, ICondition b) { _a = a; _b = b; }
    public bool Evaluate(Facts facts) => _a.Evaluate(facts) && _b.Evaluate(facts);
}
```

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
A: Technically yes, but it's discouraged. Conditions should be predicates. Mutating facts in `Evaluate` may produce surprising interactions between rules.

Q: What if the fact type doesn't match the expected type?
A: Use `TryGetFactValue<T>` to safely test and retrieve a typed value. Do not rely on `GetFactValue<T>` without validation.