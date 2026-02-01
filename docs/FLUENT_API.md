# Fluent API

This document describes the new fluent builders introduced to make programmatic rule authoring easier.

Overview
--------
The fluent API provides small builders to create conditions, actions and rules without implementing `IRule` directly. The builders are lightweight and compose into the existing engine types (`DefaultRule`, `IAction`, `ICondition`).

Key types
---------
- `RuleBuilder` - fluent construction of `IRule` instances (builds `DefaultRule`).
- `ConditionBuilder` - small helpers for creating `ICondition` instances and composing them (`And`, `Or`, `Not`).
- `ActionBuilder` - helpers to create `IAction` instances from delegates and to compose multiple actions.

Examples
--------
Programmatic rule using the fluent API:

```csharp
using LightRules.Core;

var rule = RuleBuilder.Create("OrderPositive")
    .WithDescription("Fires when order quantity is positive")
    .WithPriority(10)
    .When(f => f.TryGetFactValue<int>("quantity", out var q) && q > 0)
    .Then(f => f.AddOrReplaceFact("orderAccepted", true))
    .Build();

var rules = new Rules(rule);
```

Composing conditions:

```csharp
var cond = ConditionBuilder.And(
    ConditionBuilder.From(f => f.TryGetFactValue<int>("quantity", out var q) && q > 0),
    ConditionBuilder.Not(ConditionBuilder.From(f => f.TryGetFactValue<bool>("cancelled", out var c) && c))
);

var rule = RuleBuilder.Create()
    .When(cond)
    .Then(ActionBuilder.From(f => f.AddOrReplaceFact("accepted", true)))
    .Build();
```

Composing actions:

```csharp
var a1 = ActionBuilder.From(f => f.AddOrReplaceFact("a", 1));
var a2 = ActionBuilder.From(f => f.AddOrReplaceFact("b", 2));
var composed = ActionBuilder.Compose(new[] { a1, a2 });

var rule = RuleBuilder.Create("multi")
    .When(f => true)
    .Then(composed)
    .Build();
```

Migration notes
---------------
- The fluent API produces the same runtime types as before (`DefaultRule`, `IAction`, `ICondition`), so existing engines and discovery code continue to work.
- Prefer the fluent builders for programmatic rule creation when you don't use the attribute-based generator.

Best practices
--------------
- Keep conditions side-effect free; actions should return a new `Facts` instance when mutating state.
- Use `ConditionBuilder` combinators instead of writing complex delegate-based predicates inline for readability.

