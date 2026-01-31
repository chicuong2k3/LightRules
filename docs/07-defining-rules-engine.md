# Defining Rules Engine and Parameters

<!-- Table of contents -->
- [What is an engine?](#what-is-an-engine)
- [RulesEngineParameters](#rulesengineparameters)
- [Listeners](#listeners)
- [Typical usage](#typical-usage)
- [Behavior notes and troubleshooting](#behavior-notes-and-troubleshooting)

This document explains the core rules engine concepts in LightRules, the available engine implementations, the `RulesEngineParameters` configuration, listener hooks, and a short example for beginners.

## What is an engine?

A rules engine is the component that takes a set of `IRule` instances and a `Facts` collection and coordinates evaluation and execution. LightRules exposes an interface `IRulesEngine` and two common implementations:

- `DefaultRulesEngine`: a simple sequential engine that evaluates rules in order and executes matching rules.
- `InferenceRulesEngine`: an engine that can perform iterative inference by selecting candidate rules, executing them, and repeating until no more candidates are applicable (behavior described in its implementation).

Most users start with `DefaultRulesEngine` and move to `InferenceRulesEngine` when they need iterative rule firing.

## RulesEngineParameters

`RulesEngineParameters` controls cooperative shortcuts and thresholds used by engines. The class exposes fluent setters to configure behavior:

- `WithSkipOnFirstAppliedRule(bool)`: when true, stop evaluating further rules after the first rule has been applied.
- `WithSkipOnFirstNonTriggeredRule(bool)`: when true, stop when a rule is not triggered (evaluates to false) depending on engine semantics.
- `WithSkipOnFirstFailedRule(bool)`: when true, stop when a rule evaluation or execution throws an exception.
- `WithPriorityThreshold(int)`: set a numeric threshold; rules with `Priority` greater than this threshold are skipped.

Example:

```csharp
var parameters = new RulesEngineParameters()
    .WithSkipOnFirstAppliedRule(true)
    .WithPriorityThreshold(100);

var engine = new DefaultRulesEngine(parameters);
```

Note: The parameters are engine hints; concrete engine implementations use them according to their design. Use unit tests to verify the behavior you rely on.

## Listeners

LightRules supports two listener interfaces for instrumentation and control:

- `IRuleListener`: per-rule hooks invoked around evaluation and execution of each `IRule`.
- `IRulesEngineListener`: engine-level hooks invoked before and after evaluating a whole rule set (or a candidate set in iterative engines).

Register listeners on the engine (both `DefaultRulesEngine` and `InferenceRulesEngine` inherit helper registration methods from `AbstractRulesEngine`):

```csharp
var engine = new DefaultRulesEngine(parameters);
engine.RegisterRuleListener(new MyRuleListener());
engine.RegisterRulesEngineListener(new MyEngineListener());
```

Listeners are executed in registration order. Keep listeners lightweight and non-blocking; heavy work should be offloaded to background tasks.

## Typical usage

1. Discover or construct `IRule` instances (use `RuleDiscovery.Discover()` for generated adapters or create `DefaultRule`/`BasicRule` instances programmatically).
2. Create and populate a `Facts` instance.
3. Create `RulesEngineParameters` and instantiate the engine implementation you need.
4. Optionally register `IRuleListener` and `IRulesEngineListener` instances for tracing and metrics.
5. Call `engine.Fire(rules, facts)` (or `engine.Check(rules, facts)` depending on the API) to run the engine.

Example (end-to-end minimal):

```csharp
var rules = new Rules(); // populate with discovered or programmatic rules
var facts = new Facts();
facts = facts.Set("quantity", 5);

var parameters = new RulesEngineParameters().WithSkipOnFirstAppliedRule(true);
var engine = new DefaultRulesEngine(parameters);

// Fire returns the final Facts instance after all rules execute
var finalFacts = engine.Fire(rules, facts);
Console.WriteLine($"Final facts: {finalFacts}");
```

## Behavior notes and troubleshooting

- **Immutable Facts**: `Facts` is now immutable. Actions return a new `Facts` instance, and the engine threads the returned instance forward to subsequent rules.
- **Engine return value**: `engine.Fire(rules, facts)` returns the final `Facts` instance after all rules have executed. Capture this if you need to inspect results.
- **Priority threshold**: rules with `Priority` greater than the configured `WithPriorityThreshold` value are skipped — use this to limit execution to high-priority rules only.
- **Listener exceptions**: behavior when listeners throw depends on engine design. Prefer catching errors inside listeners to avoid affecting engine execution.
- **Iterative engines**: `InferenceRulesEngine` may call engine-level listeners multiple times for each iteration; if you rely on call counts, correlate iterations explicitly.
