# Defining Actions

<!-- Table of contents -->
- [What is an Action?](#what-is-an-action)
- [Key semantics](#key-semantics)
- [Implementing an action](#implementing-an-action)
- [Parameter binding with attribute-based actions](#parameter-binding-with-attribute-based-actions)
- [Best practices](#best-practices)
- [Examples](#examples)
- [Troubleshooting / FAQ](#troubleshooting--faq)

## What is an Action?

An Action is the piece of code executed when a rule fires (i.e., when its condition evaluated to `true`).
Actions perform work such as producing a new facts set, triggering side-effects (logging, sending messages, updating data stores), or invoking other services.

In LightRules the Action abstraction is represented by the `IAction` interface:

```csharp
public interface IAction
{
    // Execute the action and return the Facts instance to use for subsequent rules.
    Facts Execute(Facts facts);
}
```

Actions are functional: they accept a `Facts` instance and return a (possibly new) `Facts` instance. This makes the engine behaviour explicit and suitable for reasoning and testing.

## Key semantics

- Side effects: Actions may produce side effects (external calls). Prefer to keep side-effects explicit and document them.
- Facts flow: Actions return a `Facts` instance; the engine threads the returned instance forward so subsequent rules see any updates made by previous actions.

## Implementing an action

Functional style (recommended):

```csharp
public class MarkProcessedAction : IAction
{
    public Facts Execute(Facts facts)
    {
        return facts.Set("processed", true);
    }
}
```

Creating an action from a delegate:

```csharp
var action = Actions.From(f => f.Set("handled", true)); // returns Facts
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

1) Functional action returning updated Facts (recommended):

```csharp
[Action]
public Facts MarkProcessed(Facts facts)
{
    return facts.Set("processed", true);
}
```

2) Action that performs external work and sets a fact:

```csharp
public class ArchiveAction : IAction
{
    public Facts Execute(Facts facts)
    {
        if (facts.TryGetValue<string>("documentId", out var id))
        {
            ArchiveService.Archive(id);
            return facts.Set("archived", true);
        }
        return facts;
    }
}
```

3) Multiple actions with ordering:

```csharp
[Rule("BackupRule")]
public class BackupRule
{
    [Condition]
    public bool ShouldBackup([Fact("needsBackup")] bool needsBackup) => needsBackup;

    [Action(Order = 1)]
    public Facts Prepare(Facts facts) { /* prepare */ return facts; }

    [Action(Order = 2)]
    public Facts Backup(Facts facts) { /* backup */ return facts.Set("backedUp", true); }

    [Action(Order = 3)]
    public Facts Cleanup(Facts facts) { /* cleanup */ return facts; }
}
```

Note: Actions are executed in `Order` sequence. The `Facts` instance returned by each action is passed to the next.

## Async Actions

LightRules provides first-class support for asynchronous actions via the `IAsyncAction` interface:

```csharp
public interface IAsyncAction
{
    Task<Facts> ExecuteAsync(Facts facts, CancellationToken cancellationToken = default);
}
```

### Creating async actions

Use the `AsyncActions` helper class:

```csharp
// From an async function
var asyncAction = AsyncActions.From(async (facts, ct) =>
{
    await SomeAsyncOperation(ct);
    return facts.Set("completed", true);
});

// Wrap a synchronous action
var wrapped = AsyncActions.FromSync(syncAction);
```

### Using async actions with the engine

Call `FireAsync` on the engine to execute rules asynchronously:

```csharp
var engine = new DefaultRulesEngine();
var finalFacts = await engine.FireAsync(rules, facts, cancellationToken);
```

The engine automatically detects rules implementing `IAsyncRule` and uses async evaluation/execution for them.

## Troubleshooting / FAQ

Q: My action throws, what happens?
A: Engine behavior is implementation-specific. Common policies: propagate the exception, log and continue, or stop rule execution. Review your executor or add a wrapper action that handles exceptions.

Q: Can actions run asynchronously?
A: Yes! Use `IAsyncAction` for async actions and `IAsyncRule` for async rules. The engine's `FireAsync` method supports async evaluation and execution. For sync rules/actions used with `FireAsync`, they are executed synchronously but wrapped in tasks.

Q: Should I call external services directly from an action?
A: It's common but consider abstraction and testability. Prefer injecting service clients and keep actions thin wrappers that orchestrate calls. For I/O-bound operations, use `IAsyncAction` to avoid blocking threads.
