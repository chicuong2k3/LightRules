# Engine-level listeners (IRulesEngineListener)

<!-- Table of contents -->
- [Overview](#overview)
- [Interface summary](#interface-summary)
- [Key methods](#key-methods)
- [Examples](#examples)
- [Registering engine listeners](#registering-engine-listeners)
- [Best practices and caveats](#best-practices-and-caveats)

## Overview

`IRulesEngineListener` provides hooks to observe the lifecycle of a rules engine execution at the granularity of a whole rule set. These listeners are invoked before a ruleset is evaluated and after the ruleset has been executed. They are useful for tasks such as logging, metrics, preparing or validating facts before execution, or triggering post-run actions.

## Interface summary

```csharp
public interface IRulesEngineListener
{
    void BeforeEvaluate(Rules rules, Facts facts);
    void AfterExecute(Rules rules, Facts facts);
}
```

## Key methods

- `BeforeEvaluate(Rules rules, Facts facts)`
  - Called once before the engine evaluates the provided `rules` against the supplied `facts`.
  - For iterative engines (e.g., inference engines that repeatedly select candidate rules), this method may be called
    multiple times — once for each candidate set or iteration. Check your concrete engine's documentation if you rely on
    the call frequency.
  - Use this method to prepare or validate facts, start timers, create a correlation context for logs, or short-circuit external resources used during execution.

- `AfterExecute(Rules rules, Facts facts)`
  - Called after the engine completed executing the ruleset. If the engine iterates multiple times (inference engine), it may be called after each executed candidate set; check concrete engine behavior.
  - Use this to stop timers, record metrics, or persist final facts/state.

## Examples: simple logging engine listener

```csharp
public class SimpleEngineListener : IRulesEngineListener
{
    public void BeforeEvaluate(Rules rules, Facts facts)
    {
        Console.WriteLine($"[Engine] Starting evaluation of {rules.Size} rules with facts: {facts}");
    }

    public void AfterExecute(Rules rules, Facts facts)
    {
        Console.WriteLine($"[Engine] Finished execution. Facts now: {facts}");
    }
}
```

## Registering engine listeners

If you use `DefaultRulesEngine` or `InferenceRulesEngine` (both inherit from `AbstractRulesEngine`), register listeners via provided registration methods:

```csharp
var engine = new DefaultRulesEngine(parameters);
// single listener
engine.RegisterRulesEngineListener(new SimpleEngineListener());
// register multiple listeners at once
engine.RegisterRulesEngineListeners(new[] { new SimpleEngineListener(), new AnotherListener() });
```

Multiple listeners can be registered; the engine will invoke each in registration order.

## Best practices and caveats

- Keep listeners lightweight: `BeforeEvaluate`/`AfterExecute` run on the engine thread and should not block long-running I/O. Offload heavy work to background tasks if needed.
- Facts are immutable. Avoid attempting to mutate a `Facts` instance inside engine listeners — mutation helpers return a
  new `Facts` instance and do not alter the original.

  - If you need to prepare or adjust facts before evaluation, prefer one of these patterns:
    - Construct an initial `Facts` instance (via the public `Facts` helpers or a `FactsBuilder`) and pass that into the
      engine when calling `Fire`/`Evaluate`.
    - Emit a request from the listener to the caller code so the caller can create a modified `Facts` and re-run the
      engine if appropriate.

- Be mindful of inference engines: `BeforeEvaluate`/`AfterExecute` may be invoked multiple times; use a correlation id if you want to tie together iterations.
- Exceptions thrown from listeners may affect engine execution depending on the engine implementation. Prefer catching/logging exceptions inside the listener.

## When to use engine listeners

- Collecting per-run metrics (latency, applied rules count).
- Auditing rule execution over time.
- Preparing or validating a `Facts` bag (enriching it before evaluation).
- Clearing or persisting state after a run.
