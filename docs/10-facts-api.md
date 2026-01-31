# Facts API Reference

A compact reference table for the `Facts` collection API. **Note: `Facts` is immutable and thread-safe** - all mutation methods return a new `Facts` instance.

| Member | Description | Behavior / Notes |
|---|---|---|
| `facts["name"]` | Indexer to get a fact value as `object?` | Read-only. Returns `GetFactValue<object?>(name)` semantics. |
| `AddOrReplaceFact<T>(string name, T value)` | Add or replace a typed fact | **Returns new Facts instance**. Throws `ArgumentNullException` if `value` is `null`. |
| `AddFact(Fact)` / `AddFact<T>(Fact<T>)` | Add a `Fact` instance | **Returns new Facts instance**. Replaces existing fact with the same name. |
| `GetFactValue<T>(string name)` | Get the value cast to `T` | Returns `default(T)` if not present. If present but runtime type is incompatible, cast will throw. Use `TryGetFactValue<T>` when unsure. |
| `TryGetFactValue<T>(string name, out T? value)` | Safe typed read | Returns `true` only when a fact exists and its runtime value is assignable to `T`. Sets `value` accordingly. |
| `GetFact(string name)` | Retrieve the stored `Fact` object (non-generic) | Returns `null` if not present. |
| `TryGetFact(string name, out Fact? fact)` | Try to get a raw `Fact` | Returns `true` if found; `fact` contains the stored `Fact`. |
| `TryGetFact<T>(string name, out Fact<T>? fact)` | Try to get a `Fact<T>` | If stored fact value is assignable to `T`, returns a typed `Fact<T>`. |
| `RemoveFact(string name)` | Remove a fact by name | **Returns new Facts instance**. Removing a non-existing fact returns the same instance. |
| `RemoveFact(Fact)` / `RemoveFact<T>(Fact<T>)` | Remove a fact by instance | **Returns new Facts instance**. Uses fact name for lookup. |
| `ContainsFact(string name)` | Check if a fact exists | Returns `true` if a fact with the given name exists. |
| `Count` | Number of facts in the collection | Read-only property. |
| `ToDictionary()` | Shallow copy as `Dictionary<string, object?>` | Useful for logging or serialization. |
| `Clone()` | Create a copy | Since `Facts` is immutable, returns the same instance. |
| `IEnumerable<Fact>` | Enumeration over stored facts | Iterates all facts in the collection. |

## Usage Examples

```csharp
// Create and populate facts (functional/immutable style)
var facts = new Facts();
facts = facts.AddOrReplaceFact("quantity", 10);
facts = facts.AddOrReplaceFact("customer", "Alice");

// Read values
if (facts.TryGetFactValue<int>("quantity", out var qty))
{
    Console.WriteLine($"Quantity: {qty}");
}

// Check if fact exists
if (facts.ContainsFact("customer"))
{
    Console.WriteLine("Customer fact exists");
}

// Chain mutations
facts = facts
    .AddOrReplaceFact("processed", true)
    .AddOrReplaceFact("timestamp", DateTime.UtcNow);

// Remove a fact
facts = facts.RemoveFact("customer");
```

## Notes

- Fact name matching is case-sensitive (ordinal comparison).
- Since `Facts` is immutable, actions must return the updated `Facts` instance for changes to be visible to subsequent rules.
- Prefer `TryGetFactValue<T>` in conditions to avoid invalid cast exceptions.
- The engine's `Fire` method returns the final `Facts` instance after all rules have executed.
- `Facts` is thread-safe and can be safely shared across multiple threads.
