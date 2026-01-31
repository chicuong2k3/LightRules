# Defining Actions

This document explains what an "action" is in LightRules. It is written for beginners 
and assumes no prior knowledge of rule engines.

## What is an Action?

An Action is the piece of code executed when a rule fires (i.e., when its condition evaluated to `true`). 
Actions perform work such as changing facts, triggering side-effects (logging, sending messages, updating data stores), or invoking other services.

In LightRules the Action abstraction is represented by the `IAction` interface and the `Actions` static helper:

```csharp
public interface IAction
{
    void Execute(Facts facts);
}
```

There is also a functional helper to create actions from delegates. The engine calls `Execute` with the current `Facts` instance when the rule fires.

## Key semantics

- Side effects are allowed: Actions exist to produce side effects. Unlike conditions (which should be pure when possible), actions commonly mutate facts or interact with external systems.
- Facts instance: Actions receive the same `Facts` instance used during condition evaluation. 
Mutating this `Facts` object affects subsequent rules executed in the same engine run 
unless the engine snapshots facts beforehand.
- Exceptions: Action implementations may throw exceptions. The engine's policy determines how exceptions are handled (propagation, logging, skipping remaining actions, or aborting rule execution). 
When possible, handle recoverable errors inside the action and let unrecoverable errors bubble up only if intended.
- Ordering: A rule can expose multiple actions. Actions should run in the defined order. When writing multiple actions, keep them independent when possible.

## Implementing an action

You can implement `IAction` directly for reusable actions:

```csharp
public class SendNotificationAction : IAction
{
    public void Execute(Facts facts)
    {
        if (facts.TryGetValue<string>("email", out var email))
        {
            // send email (pseudo-code)
            EmailClient.Send(email, "Alert", "A rule fired");
            facts.Set("notificationSent", true);
        }
    }
}
```

Or use a delegate-based helper to create small actions inline:

```csharp
var action = Actions.From(f => f.Set("handled", true));
```

## Parameter binding with attribute-based actions

If your rules use attribute-based method discovery, the engine may support binding method parameters from facts using a `[Fact("name")]` attribute, similar to conditions. Example:

```csharp
[Action]
public void Send([Fact("email")] string email)
{
    EmailClient.Send(email, "Hi", "...body...");
}
```

The engine is responsible for resolving parameters before calling the method. For optional facts or type mismatches, prefer nullable parameters or use `Facts` directly.

## Best practices

- Keep actions small: Prefer composable, single-responsibility actions. This makes testing and ordering simpler.
- Be explicit about side effects: Document what state the action changes (facts, external systems). Avoid hidden global side effects.
- Idempotence: When actions interact with external systems, design them to be idempotent where possible  repeated execution should not cause incorrect duplicate effects.
- Error handling: Catch and handle expected exceptions inside the action. Use structured logging and consider retry/backoff strategies outside of the rule engine unless the engine integrates retries.
- Mutating facts: If you mutate facts in an action, understand the effect on subsequent rules. If you need isolation, either clone the `Facts` instance beforehand or ensure the executor snapshots facts.

## Examples

1) Simple inline action using `Facts`:

```csharp
[Action]
public void MarkProcessed(Facts facts)
{
    facts.Set("processed", true);
}
```

2) Action that performs external work and sets a fact:

```csharp
public class ArchiveAction : IAction
{
    public void Execute(Facts facts)
    {
        if (facts.TryGetValue<string>("documentId", out var id))
        {
            ArchiveService.Archive(id);
            facts.Set("archived", true);
        }
    }
}
```

3) Multiple actions and order (illustrative):

```csharp
[Rule("BackupRule")]
public class BackupRule : IRule
{
    [Action(Order = 1)]
    public void Prepare(Facts facts) { /* prepare */ }

    [Action(Order = 2)]
    public void Backup(Facts facts) { /* backup */ }

    [Action(Order = 3)]
    public void Cleanup(Facts facts) { /* cleanup */ }

    // Condition + metadata omitted for brevity
}
```

Note: The actual `Order` attribute and enforcement may vary depending on the rule discovery/executor implementation. Consult the discovery/engine docs for exact behavior.

## Troubleshooting / FAQ

Q: My action throws  what happens?
A: Engine behavior is implementation-specific. Common policies: propagate the exception, log and continue, or stop rule execution. Review your executor or add a wrapper action that handles exceptions.

Q: Can actions run asynchronously?
A: The core `IAction.Execute(Facts)` is synchronous. If you need asynchronous work, either call async methods with proper waiting (e.g., `Task.Run(...).GetAwaiter().GetResult()`) or adapt your engine to support async actions. Be mindful of blocking threads.

Q: Should I call external services directly from an action?
A: It's common but consider abstraction and testability. Prefer injecting service clients and keep actions thin wrappers that orchestrate calls.

## Where to look next

- `docs/defining-facts.md`  how facts are stored and accessed.
- `docs/defining-conditions.md`  guides for writing conditions.
- `docs/defining-rules.md`  how to combine conditions and actions into rules and how discovery works.
