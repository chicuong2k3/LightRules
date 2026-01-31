# Defining Engine Parameters and Listeners

<!-- Table of contents -->
- [Engine parameters](#engine-parameters)
- [Listeners](#listeners)
- [Order and typical usage](#order-and-typical-usage)

This document describes the engine-level parameters and listener interfaces used by LightRules. It explains how to configure execution behavior via `RulesEngineParameters` and how to observe engine lifecycle events using listeners.

## Engine parameters

`RulesEngineParameters` controls execution shortcuts and thresholds used by rule engines. The class exposes the following properties:

- `SkipOnFirstAppliedRule`: if true, stop evaluating further applicable rules as soon as one rule is applied.
- `SkipOnFirstNonTriggeredRule`: if true, stop when a rule is not triggered.
- `SkipOnFirstFailedRule`: if true, stop when a rule throws/fails.
- `PriorityThreshold`: skip rules whose priority exceeds this threshold (default: no threshold).

Example:

```csharp
var parameters = new RulesEngineParameters()
    .WithSkipOnFirstAppliedRule(true)
    .WithPriorityThreshold(100);
```

## Listeners

Listeners let you observe and react to engine events:

- `IRuleListener` (not implemented here) typically observes per-rule events (before/after evaluate/execute).
- `IRulesEngineListener` observes engine-level events such as before/after evaluating a whole rule set.

`IRulesEngineListener` methods:

- `BeforeEvaluate(Rules rules, Facts facts)`: invoked before evaluating the provided rule set. For iterative engines this may be invoked many times (once per candidate set).
- `AfterExecute(Rules rules, Facts facts)`: invoked after executing the provided rule set.

Example listener:

```csharp
public class LoggingEngineListener : IRulesEngineListener
{
    public void BeforeEvaluate(Rules rules, Facts facts)
    {
        Console.WriteLine($"About to evaluate {rules.Size()} rules with facts: {facts}");
    }

    public void AfterExecute(Rules rules, Facts facts)
    {
        Console.WriteLine($"Finished executing rules. Facts now: {facts}");
    }
}
```

## Order and typical usage

1. Create or discover rules (via `RuleDiscovery`).
2. Create `Facts` and populate them.
3. Construct engine parameters and the engine instance.
4. Optionally register listeners.
5. Fire or check rules using the engine.

This document complements `docs/defining-rules.md` which describes rule structure and discovery.
