# Defining Rules

<!-- Table of contents -->
- [Overview](#overview)
- [How to define rules](#how-to-define-rules)
- [Attribute-driven (declarative)](#how-to-define-rules)
- [Discovery](#discovery)
- [Examples](#examples)
- [Best practices and notes](#best-practices-and-notes)

This document explains how to define rules in LightRules, the available APIs and attributes, how rules are discovered, and simple examples for beginners. It assumes no prior knowledge of rule engines.

## Overview

A rule is a named piece of business logic that can be evaluated against a set of facts and, when its conditions hold, perform actions. A rule typically contains:
- Name: a unique identifier within a rules collection.
- Description: a human-friendly summary of what the rule does.
- Priority: an ordering value used when multiple rules are evaluated; lower numbers run first.
- Conditions: predicates evaluated against the current facts to decide whether the rule should fire.
- Actions: operations executed when a rule fires; actions may mutate facts or cause side effects.

In LightRules a rule is represented by the `IRule` interface:

```csharp
public interface IRule : IComparable<IRule>
{
    public const string DefaultName = "rule";
    public const string DefaultDescription = "";
    public const int DefaultPriority = int.MaxValue - 1;

    string Name { get; }
    string Description { get; }
    int Priority { get; }

    // Evaluate the rule against a set of facts. Return true to fire the rule.
    bool Evaluate(Facts facts);

    // Execute actions using the provided facts. Invoked when Evaluate returns true.
    void Execute(Facts facts);
}
```

Note: `Facts` is the lightweight data/context container used throughout the engine. See `docs/defining-facts.md` for details.

## How to define rules

There are two common approaches:

1. Attribute-driven (declarative): decorate a POCO that implements `IRule` with attributes that mark condition and action methods. A discovery + injector component can then build and invoke rules automatically.
2. Programmatic: implement `IRule` (or use helper classes like `BasicRule` / `DefaultRule`) and register instances with the `Rules` collection.

### Attribute-driven (declarative) source-generator (recommended)

<a name="attribute-driven-declarative"></a>

LightRules supports a generator-backed attribute approach: write plain POCO rule classes annotated with `Rule`, `Condition`, `Action`, `Fact`, and `Priority` attributes and let the source generator produce high-performance adapters at compile time.

See `docs/defining-rules-attribute-based.md` for a full guide, examples, and migration notes.

### Attributes (declarative rules)

Attributes live in the `LightRules.Attributes` namespace and declare the shape of a rule. With the source-generator approach described above, attribute metadata is consumed at compile-time by the generator which emits concrete `IRule` adapters. Runtime reflection and dynamic proxies are not required; the generated adapters call your POCO's methods directly.

#### Key attributes

- `RuleAttribute` (class)
  - Optional metadata: `Name`, `Description`, `Priority`, `Enabled` (default true), `Tags`.
  - Constructors: `RuleAttribute()`, `RuleAttribute(string name)`, `RuleAttribute(string name, int priority)`.

- `ConditionAttribute` (method)
  - Marks the method that evaluates the rule's condition. The method must return `bool`.
  - The method may accept parameters annotated with `[Fact("name")]` to indicate binding from `Facts`.

- `ActionAttribute` (method)
  - Marks a method to execute when the rule fires. Optional `Order` (constructor `ActionAttribute(int)` or `ActionAttribute()`) controls execution order when multiple actions exist.

- `FactAttribute` (parameter)
  - Applied to method parameters to indicate which fact name should be injected. If omitted, the generator falls back to the parameter name.

- `PriorityAttribute` (method)
  - Marks an instance method that returns an `int` used to compute the rule's priority dynamically at compile time (the adapter will call it at runtime).

Important: attributes only declare intent. The source generator consumes the attributes and emits adapters and a registry (no runtime scanning or reflection required). The runtime engine uses the generated adapters directly.

### Discovery

LightRules provides a simple discovery helper:

- `RuleDiscovery.Discover()` scans the assemblies loaded into the current process (the same assemblies the generator emitted `LightRules.Generated.RuleRegistry` into) and returns an ordered list of `RuleMetadata` objects describing generated adapters.

`RuleMetadata` includes:
- `Type RuleType`
- `string Name`
- `string? Description`
- `int Priority`
- `bool Enabled`
- `IReadOnlyList<string> Tags`

`RuleDiscovery` orders results by priority. Discovery is metadata-only: you still need an injector (or the engine) to create instances, bind method parameters from `Facts`, and invoke condition/action methods.

> Note: `RuleDiscovery.Discover()` does not construct adapter instances; it returns `RuleMetadata` values that point to the generated adapter types (for example `MyRule_RuleAdapter`). The adapter constructors may vary (parameterless or accepting the original POCO). A robust injector should try creating an adapter by passing the POCO and fall back to a parameterless constructor.

### BasicRule, DefaultRule and Rules (programmatic helpers)

- `BasicRule`: a small base implementation of `IRule` that provides `Name`, `Description`, `Priority`, and default no-op `Evaluate` / `Execute` implementations. Extend it when you want a simple rule base class.
- `DefaultRule`: a convenient rule implementation that wraps an `ICondition` and a list of `IAction`. It evaluates the condition and runs all actions in order when the condition is true.

- `Rules`: a collection that holds and orders rules. Rules are ordered by `Priority` (ascending) and then by `Name` (ordinal, case-insensitive). `Rules` provides registration and unregistration helpers.

## Examples

1) Attribute-based rule (declarative)

```csharp
using LightRules.Attributes;
using LightRules.Core;

[Rule("OrderPositiveRule", Priority = 10, Description = "Fires when order quantity is positive", Enabled = true, Tags = new[] { "orders", "validation" })]
public class OrderPositiveRule : IRule
{
    [Condition]
    public bool Check([Fact("quantity")] int quantity)
    {
        return quantity > 0;
    }

    [Action(1)]
    public void OnSuccess(Facts facts)
    {
        facts.Set("orderAccepted", true);
    }

    // IRule metadata and simple delegation
    public string Name => "OrderPositiveRule";
    public string Description => "Fires when order quantity is positive";
    public int Priority => 10;

    public bool Evaluate(Facts facts)
    {
        // Inline binding example (a real injector would do this automatically)
        return facts.TryGetValue<int>("quantity", out var q) && Check(q);
    }

    public void Execute(Facts facts)
    {
        OnSuccess(facts);
    }
}
```

2) Programmatic rule using `DefaultRule`

```csharp
var condition = Conditions.From(f => f.TryGetValue<int>("quantity", out var q) && q > 0);
var actions = new List<IAction> { Actions.From(f => f.Set("orderAccepted", true)) };
var rule = new DefaultRule("OrderPositiveRule", "Fires when order quantity is positive", 10, condition, actions);

var rules = new Rules(rule);
```

### Simple injector snippet

Below is a tiny example to illustrate how a discovery+injector could work. It's intentionally simple  production injectors should handle conversions, missing facts, optional parameters, and errors.

```csharp
// Discover rule metadata across loaded assemblies
var metas = RuleDiscovery.Discover();

// Instantiate and bind rule adapter instances (very small example)
// The generator produces adapter types (e.g. MyRule_RuleAdapter). Use the metadata to create adapter instances.
var ruleInstances = new List<IRule>();
foreach (var meta in metas)
{
    // meta.RuleType is the generated adapter type; adapter constructors may accept the original POCO or be parameterless.
    IRule instance;
    try
    {
        // try constructor that accepts the original POCO (if you have it)
        instance = (IRule)Activator.CreateInstance(meta.RuleType /*, pocoInstance */)!;
    }
    catch
    {
        // fallback to parameterless constructor
        instance = (IRule)Activator.CreateInstance(meta.RuleType)!;
    }
    ruleInstances.Add(instance);
}

// Evaluate/execute loop with Facts
var facts = new Facts();
facts.Set("quantity", 5);

var rulesCollection = new Rules(ruleInstances);
foreach (var r in rulesCollection)
{
    if (r.Evaluate(facts))
    {
        r.Execute(facts);
    }
}
```

## Best practices and notes

- Prefer `Facts.TryGetValue<T>` when reading facts in conditions to avoid invalid cast exceptions.
- Keep conditions side effect free. Actions are the place to mutate facts or call external systems.
- Mutating `Facts` inside actions affects subsequent rules in the same run  LightRules engines do not snapshot facts by default. If you need isolation, clone the `Facts` instance before firing rules or use a custom executor.
- Use `ActionAttribute(Order = n)` to express action ordering when multiple methods exist on a rule (discovery/invoker must respect this).
- Use `RuleDiscovery` for simple discovery; if you need method/parameter binding create or reuse a small injector that reads attributes and invokes methods.
