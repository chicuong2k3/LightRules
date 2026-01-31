# Per-rule listeners (IRuleListener)

<!-- Table of contents -->
- [Overview](#overview)
- [Interface summary](#interface-summary)
- [Method semantics](#method-semantics)
- [Examples](#examples)
- [Registering rule listeners](#registering-rule-listeners)
- [Best practices](#best-practices)

This document describes the `IRuleListener` interface and how to use per-rule listeners to observe and react to events that occur while a specific rule is evaluated and executed.

## Overview

`IRuleListener` provides hooks at the rule level: before/after condition evaluation, on evaluation errors, before execution, on success and on failure. Use per-rule listeners to implement custom logging, testing hooks, metrics per rule, retry or compensation strategies, and richer observability of rule behavior.

## Interface summary

```csharp
public interface IRuleListener
{
    bool BeforeEvaluate(IRule rule, Facts facts);
    void AfterEvaluate(IRule rule, Facts facts, bool evaluationResult);
    void OnEvaluationError(IRule rule, Facts facts, Exception exception);

    void BeforeExecute(IRule rule, Facts facts);
    void OnSuccess(IRule rule, Facts facts);
    void OnFailure(IRule rule, Facts facts, Exception exception);
}
```

## Method semantics

- `BeforeEvaluate(IRule rule, Facts facts)`
  - Called before a rule's condition is evaluated.
  - Return `true` to allow evaluation to proceed or `false` to skip evaluating this rule.
  - Useful for conditional short-circuiting (skip evaluation based on external criteria) or for pre-evaluation instrumentation.

- `AfterEvaluate(IRule rule, Facts facts, bool evaluationResult)`
  - Called after the condition has been evaluated. `evaluationResult` reflects the boolean outcome.
  - Use this to record the evaluation result or to adjust metrics.

- `OnEvaluationError(IRule rule, Facts facts, Exception exception)`
  - Called when the condition evaluation throws an exception.
  - The listener can log, report, or correlate the exception. The engine decides how to proceed (skip, abort, or treat as false) according to its parameters.

- `BeforeExecute(IRule rule, Facts facts)`
  - Called before executing a rule's actions. Good place to set up an execution context.

- `OnSuccess(IRule rule, Facts facts)`
  - Called after the actions have completed successfully.

- `OnFailure(IRule rule, Facts facts, Exception exception)`
  - Called when executing the actions results in an exception.
  - Listeners can attempt compensating actions, report errors or collect diagnostics.

## Examples: recording basic metrics per rule

```csharp
public class MetricsRuleListener : IRuleListener
{
    public bool BeforeEvaluate(IRule rule, Facts facts)
    {
        // increment evaluation counter
        Metrics.Increment("rule.evaluations", rule.Name);
        return true;
    }

    public void AfterEvaluate(IRule rule, Facts facts, bool evaluationResult)
    {
        Metrics.Record("rule.evaluation.result", evaluationResult ? 1 : 0, rule.Name);
    }

    public void OnEvaluationError(IRule rule, Facts facts, Exception exception)
    {
        Metrics.Increment("rule.evaluation.errors", rule.Name);
    }

    public void BeforeExecute(IRule rule, Facts facts) { }
    public void OnSuccess(IRule rule, Facts facts) { Metrics.Increment("rule.executions.success", rule.Name); }
    public void OnFailure(IRule rule, Facts facts, Exception exception) { Metrics.Increment("rule.executions.failure", rule.Name); }
}
```

## Registering rule listeners

`AbstractRulesEngine` provides helper methods to register one or more `IRuleListener` instances. For example:

```csharp
var engine = new DefaultRulesEngine();
// register a single listener
engine.RegisterRuleListener(new MetricsRuleListener());
// or register multiple listeners at once
engine.RegisterRuleListeners(new[] { new MetricsRuleListener(), new AnotherListener() });
```

## Best practices

- Keep listeners non-blocking and lightweight. Heavy I/O should be offloaded to background tasks.
- Avoid mutating `Facts` as a side-effect in `BeforeEvaluate`/`AfterEvaluate` unless intentional; this can impact subsequent evaluations.
- Use `BeforeEvaluate`'s boolean return value to implement dynamic skipping rules (for example based on runtime feature flags).
- Catch and handle listener exceptions where appropriate; listener exceptions should not crash the engine unless intentionally designed to do so.

## Common uses

- Logging evaluation/execution traces for debugging or auditing.
- Gathering per-rule metrics (times, success rates, error counts).
- Integrating with distributed tracing to attach rule evaluation/execution spans.
- Implementing custom retry/compensation policies tied to rule execution failures.
