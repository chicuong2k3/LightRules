# Defining Actions

<!-- Table of contents -->
- [What is an Action?](#what-is-an-action)
- [Implementing an action](#implementing-an-action)
- [Parameter binding](#parameter-binding)
- [Multiple actions with ordering](#multiple-actions-with-ordering)
- [Async actions](#async-actions)
- [Best practices](#best-practices)
- [Error handling](#error-handling)
- [FAQ](#faq)

## What is an Action?

An Action is the piece of code executed when a rule fires (i.e., when its condition evaluated to `true`).
Actions perform work such as producing a new facts set, triggering side-effects (logging, sending messages, updating data stores), or invoking other services.

In LightRules the Action abstraction is represented by the `IAction` interface:

```csharp
public interface IAction
{
    Facts Execute(Facts facts);
}
```

Actions are functional: they accept a `Facts` instance and return a (possibly new) `Facts` instance. This makes the engine behaviour explicit and suitable for reasoning and testing.

## Implementing an action

### Class-based action

```csharp
public class MarkProcessedAction : IAction
{
    public Facts Execute(Facts facts)
    {
        return facts.AddOrReplaceFact("processed", true);
    }
}
```

### Delegate-based action

```csharp
var action = Actions.From(f => f.AddOrReplaceFact("handled", true));
```

### Attribute-based action (in a rule class)

```csharp
[Rule("ProcessOrder")]
public class ProcessOrderRule
{
    [Condition]
    public bool ShouldProcess([Fact("status")] string status) => status == "pending";

    [Action]
    public Facts Process(Facts facts)
    {
        return facts.AddOrReplaceFact("status", "processed");
    }
}
```

## Parameter binding

Action methods can use `[Fact("name")]` attribute on parameters for automatic binding:

```csharp
[Action]
public Facts SendEmail([Fact("email")] string email, Facts facts)
{
    EmailClient.Send(email, "Subject", "Body");
    return facts.AddOrReplaceFact("emailSent", true);
}
```

## Multiple actions with ordering

Use the `Order` parameter to control execution sequence:

```csharp
[Rule("BackupRule")]
public class BackupRule
{
    [Condition]
    public bool ShouldBackup([Fact("needsBackup")] bool needsBackup) => needsBackup;

    [Action(Order = 1)]
    public Facts Prepare(Facts facts)
    {
        // Prepare for backup
        return facts.AddOrReplaceFact("backupPrepared", true);
    }

    [Action(Order = 2)]
    public Facts Backup(Facts facts)
    {
        // Perform backup
        return facts.AddOrReplaceFact("backedUp", true);
    }

    [Action(Order = 3)]
    public Facts Cleanup(Facts facts)
    {
        // Cleanup temporary files
        return facts.AddOrReplaceFact("cleanedUp", true);
    }
}
```

Actions are executed in `Order` sequence. The `Facts` instance returned by each action is passed to the next.

## Async actions

LightRules provides support for asynchronous actions via the `IAsyncAction` interface:

```csharp
public interface IAsyncAction
{
    Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default);
}
```

### Creating async actions

```csharp
// From an async function
var asyncAction = AsyncActions.From(async (facts, ct) =>
{
    await SomeAsyncOperation(ct);
    return facts.AddOrReplaceFact("completed", true);
});

// Wrap a synchronous action
var wrapped = AsyncActions.FromSync(syncAction);
```

### Example async action class

```csharp
public class FetchDataAction : IAsyncAction
{
    private readonly HttpClient _client;

    public FetchDataAction(HttpClient client)
    {
        _client = client;
    }

    public async Task<Facts> ExecuteAsync(Facts facts, CancellationToken ct = default)
    {
        if (facts.TryGetFactValue<string>("url", out var url))
        {
            var response = await _client.GetStringAsync(url, ct);
            return facts.AddOrReplaceFact("response", response);
        }
        return facts;
    }
}
```

### Using async actions with the engine

```csharp
var engine = new DefaultRulesEngine();
var finalFacts = await engine.FireAsync(rules, facts, cancellationToken);
```

## Best practices

### Keep actions small and focused

Write composable actions that do one thing well. Small actions are easier to test and reason about.

```csharp
// Good: Single responsibility
[Action(Order = 1)]
public Facts ValidateOrder(Facts facts) => facts.AddOrReplaceFact("validated", true);

[Action(Order = 2)]
public Facts CalculateTotal(Facts facts) => facts.AddOrReplaceFact("total", ComputeTotal(facts));

[Action(Order = 3)]
public Facts SendConfirmation(Facts facts) => facts.AddOrReplaceFact("confirmed", true);
```

### Be explicit about side effects

Document what external systems an action touches. Avoid hidden global side effects.

```csharp
/// <summary>
/// Sends order confirmation email.
/// Side effects: Sends email via SMTP, logs to audit table.
/// Modifies facts: Sets "emailSent" to true.
/// </summary>
[Action]
public Facts SendOrderConfirmation(Facts facts) { /* ... */ }
```

### Design for idempotence

When actions interact with external systems, design them so repeated execution does not cause incorrect duplicate effects:

```csharp
[Action]
public Facts ProcessPayment(Facts facts)
{
    if (facts.TryGetFactValue<string>("paymentId", out var paymentId))
    {
        // Use idempotency key to prevent duplicate charges
        var idempotencyKey = $"order-{facts.GetFactValue<string>("orderId")}";
        PaymentService.Charge(paymentId, idempotencyKey);
        return facts.AddOrReplaceFact("paymentProcessed", true);
    }
    return facts;
}
```

Techniques for idempotence:
- Use idempotency keys for external API calls
- Use upserts instead of inserts for database operations
- Check-before-write patterns
- Store processed IDs to prevent reprocessing

### Use async for I/O operations

For HTTP calls, database queries, or file operations, use `IAsyncAction` and `FireAsync`:

```csharp
public class SaveToDbAction : IAsyncAction
{
    public async Task<Facts> ExecuteAsync(Facts facts, CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
        return facts.AddOrReplaceFact("saved", true);
    }
}
```

## Error handling

### Built-in listener support

The engine notifies listeners when actions fail:

```csharp
public class LoggingListener : IRuleListener
{
    public void OnFailure(IRule rule, Facts facts, Exception exception)
    {
        _logger.LogError(exception, "Rule {RuleName} failed", rule.Name);
    }

    // ... other methods
}

var engine = new DefaultRulesEngine();
engine.RegisterRuleListener(new LoggingListener());
```

### Engine parameters for error behavior

```csharp
var parameters = new RulesEngineParameters()
    .WithSkipOnFirstFailedRule(true); // Stop processing on first error

var engine = new DefaultRulesEngine(parameters);
```

### Exception handling in actions

Handle expected exceptions inside the action:

```csharp
[Action]
public Facts SendNotification(Facts facts)
{
    try
    {
        NotificationService.Send(facts.GetFactValue<string>("message"));
        return facts.AddOrReplaceFact("notificationSent", true);
    }
    catch (NotificationException ex)
    {
        _logger.LogWarning(ex, "Failed to send notification");
        return facts.AddOrReplaceFact("notificationFailed", true);
    }
}
```

For transient failures, consider retry/backoff outside the rule engine or use a dedicated retry wrapper.

## FAQ

**Q: My action throws, what happens?**

Engine behavior depends on configuration. By default, the exception is caught, listeners are notified via `OnFailure`, and the engine continues to the next rule. Use `WithSkipOnFirstFailedRule(true)` to stop on first error.

**Q: Should I call external services directly from an action?**

It's common but consider abstraction and testability. Prefer injecting service clients and keep actions as thin wrappers. For I/O-bound operations, use `IAsyncAction` to avoid blocking threads.

**Q: How do I share state between actions?**

Return updated `Facts` from each action. The engine passes the returned `Facts` to subsequent actions and rules.

```csharp
[Action(Order = 1)]
public Facts Step1(Facts facts) => facts.AddOrReplaceFact("step1Result", "value1");

[Action(Order = 2)]
public Facts Step2(Facts facts)
{
    var step1Result = facts.GetFactValue<string>("step1Result");
    return facts.AddOrReplaceFact("step2Result", $"processed: {step1Result}");
}
```
