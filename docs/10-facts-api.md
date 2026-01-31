# Facts API Reference

A compact reference table for the `Facts` collection API. **Note: `Facts` is immutable** - all mutation-like methods return a new `Facts` instance.

| Member | Description | Behavior / Notes |
|---|---|---|
| `facts["name"]` | Indexer to get a fact value as `object?` | Read-only. Returns `Get<object?>(name)` semantics. |
| `Set<T>(string name, T value)` | Add or replace a typed fact | **Returns new Facts instance**. Throws `ArgumentNullException` if `value` is `null`. |
| `Add(Fact)` / `Add<T>(Fact<T>)` | Add a `Fact` instance | **Returns new Facts instance**. Replaces existing fact with the same name. |
| `Get<T>(string name)` | Get the value cast to `T` | Returns `default(T)` if not present. If present but runtime type is incompatible, cast will throw. Use `TryGetValue<T>` when unsure. |
| `TryGetValue<T>(string name, out T? value)` | Safe typed read | Returns `true` only when a fact exists and its runtime value is assignable to `T`. Sets `value` accordingly. |
| `GetFact(string name)` | Retrieve the stored `Fact` object (non-generic) | Returns `null` if not present. |
| `TryGetFact(string name, out Fact? fact)` | Try to get a raw `Fact` | Returns `true` if found; `fact` contains the stored `Fact`. |
| `TryGetFact<T>(string name, out Fact<T>? fact)` | Try to get a `Fact<T>` | If stored fact value is assignable to `T`, returns a typed `Fact<T>`. |
| `Remove(string name)` | Remove a fact by name | **Returns new Facts instance**. Removing a non-existing fact returns the same instance. |
| `Remove(Fact)` / `Remove<T>(Fact<T>)` | Remove a fact by instance | **Returns new Facts instance**. Uses fact name for lookup. |
| `ToDictionary()` | Shallow copy as `Dictionary<string, object?>` | Useful for logging or serialization. |
| `Clone()` | Create a copy | Since `Facts` is immutable, returns the same instance. |
| `IEnumerable<Fact>` | Enumeration over stored facts | Iterates all facts in the collection. |

## Usage Examples

```csharp
// Create and populate facts (functional/immutable style)
var facts = new Facts();
facts = facts.Set("quantity", 10);
facts = facts.Set("customer", "Alice");

// Read values
if (facts.TryGetValue<int>("quantity", out var qty))
{
    Console.WriteLine($"Quantity: {qty}");
}

// Chain mutations
facts = facts
    .Set("processed", true)
    .Set("timestamp", DateTime.UtcNow);

// Remove a fact
facts = facts.Remove("customer");
```

## Notes

- Fact name matching is case-sensitive (ordinal comparison).
- Since `Facts` is immutable, actions must return the updated `Facts` instance for changes to be visible to subsequent rules.
- Prefer `TryGetValue<T>` in conditions to avoid invalid cast exceptions.
- The engine's `Fire` method returns the final `Facts` instance after all rules have executed.

