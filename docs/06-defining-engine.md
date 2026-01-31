# Defining Engine Parameters and Listeners

<!-- Table of contents -->
- [Engine parameters](#engine-parameters)
- [Listeners](#listeners)
  - [IRuleListener](#irulelistener)
  - [IRulesEngineListener](#irulesenginelistener)
- [Registering listeners](#registering-listeners)
- [Typical usage](#typical-usage)

## Engine parameters

`RulesEngineParameters` controls execution shortcuts and thresholds used by rule engines. The class exposes the following properties:

| Property | Description |
|----------|-------------|
| `SkipOnFirstAppliedRule` | If true, stop evaluating further rules after one rule is applied. |
| `SkipOnFirstNonTriggeredRule` | If true, stop when a rule's condition evaluates to false. |
| `SkipOnFirstFailedRule` | If true, stop when a rule throws an exception. |
| `PriorityThreshold` | Skip rules whose priority exceeds this threshold. |

Example:

```csharp
var parameters = new RulesEngineParameters()
    .WithSkipOnFirstAppliedRule(true)
    .WithPriorityThreshold(100);

var engine = new DefaultRulesEngine(parameters);
```

## Listeners

LightRules provides two listener interfaces for observing and reacting to engine events.

### IRuleListener

`IRuleListener` observes per-rule events during evaluation and execution:

| Method | Description |
|--------|-------------|
| `BeforeEvaluate(IRule, Facts)` | Called before evaluating a rule. Return `false` to skip the rule. |
| `AfterEvaluate(IRule, Facts, bool)` | Called after evaluating a rule with the evaluation result. |
| `OnEvaluationError(IRule, Facts, Exception)` | Called when evaluation throws an exception. |
| `BeforeExecute(IRule, Facts)` | Called before executing a rule (when condition is true). |
| `OnSuccess(IRule, Facts)` | Called after a rule executes successfully. |
| `OnFailure(IRule, Facts, Exception)` | Called when execution throws an exception. |

Example:

```csharp
public class LoggingRuleListener : IRuleListener
{
    public bool BeforeEvaluate(IRule rule, Facts facts)
    {
        Console.WriteLine($"Evaluating rule: {rule.Name}");
        return true; // Return false to skip this rule
    }

    public void AfterEvaluate(IRule rule, Facts facts, bool evaluationResult)
    {
        Console.WriteLine($"Rule {rule.Name} evaluated to: {evaluationResult}");
    }

    public void OnEvaluationError(IRule rule, Facts facts, Exception ex)
    {
        Console.WriteLine($"Rule {rule.Name} evaluation error: {ex.Message}");
    }

    public void BeforeExecute(IRule rule, Facts facts)
    {
        Console.WriteLine($"Executing rule: {rule.Name}");
    }

    public void OnSuccess(IRule rule, Facts facts)
    {
        Console.WriteLine($"Rule {rule.Name} executed successfully");
    }

    public void OnFailure(IRule rule, Facts facts, Exception ex)
    {
        Console.WriteLine($"Rule {rule.Name} execution failed: {ex.Message}");
    }
}
```

### IRulesEngineListener

`IRulesEngineListener` observes engine-level events for the entire rule set:

| Method | Description |
|--------|-------------|
| `BeforeEvaluate(Rules, Facts)` | Called before evaluating the rule set. |
| `AfterExecute(Rules, Facts)` | Called after executing the rule set. |

Example:

```csharp
public class LoggingEngineListener : IRulesEngineListener
{
    public void BeforeEvaluate(Rules rules, Facts facts)
    {
        Console.WriteLine($"Starting evaluation of {rules.Size} rules");
    }

    public void AfterExecute(Rules rules, Facts facts)
    {
        Console.WriteLine($"Finished executing rules. Final facts: {facts}");
    }
}
```

## Registering listeners

Register listeners on the engine instance:

```csharp
var engine = new DefaultRulesEngine(parameters);

// Register rule-level listener
engine.RegisterRuleListener(new LoggingRuleListener());

// Register engine-level listener
engine.RegisterRulesEngineListener(new LoggingEngineListener());

// Fire rules
var finalFacts = engine.Fire(rules, facts);
```

Multiple listeners can be registered; they are called in registration order.

## Typical usage

1. Create or discover rules via `RuleDiscovery.Discover()`.
2. Create `Facts` and populate with initial data.
3. Construct `RulesEngineParameters` and the engine instance.
4. Register listeners for logging, metrics, or control flow.
5. Fire rules using `engine.Fire(rules, facts)` or `engine.FireAsync(rules, facts)`.

```csharp
// Complete example
var rules = new Rules();
foreach (var meta in RuleDiscovery.Discover())
{
    rules.Register(meta.CreateInstance());
}

var facts = new Facts()
    .AddOrReplaceFact("orderId", "ORD-123")
    .AddOrReplaceFact("orderTotal", 1500m);

var parameters = new RulesEngineParameters()
    .WithSkipOnFirstFailedRule(true);

var engine = new DefaultRulesEngine(parameters);
engine.RegisterRuleListener(new LoggingRuleListener());

var finalFacts = engine.Fire(rules, facts);
```
