# Facts API Reference

A compact reference table for the `Facts` collection API.

| Member | Description | Behavior / Notes |
|---|---|---|
| `facts["name"]` | Indexer to get/set a fact value as `object?` | Setting to `null` removes the fact. Getting returns `Get<object?>(name)` semantics. |
| `Set<T>(string name, T value)` | Add or replace a typed fact (`Fact<T>`) | Throws `ArgumentNullException` if `value` is `null`. Replaces existing fact with same name. |
| `Add(Fact)` / `Add<T>(Fact<T>)` | Add a `Fact` instance | Replaces existing fact with the same name. Throws on null argument. |
| `Get<T>(string name)` | Get the value cast to `T` | Returns `default(T)` if not present. If present but runtime type is incompatible, cast will throw. Use `TryGetValue<T>` when unsure. |
| `TryGetValue<T>(string name, out T? value)` | Safe typed read | Returns `true` only when a fact exists and its runtime value is assignable to `T`. Sets `value` accordingly. |
| `GetFact(string name)` | Retrieve the stored `Fact` object (non-generic) | Throws `ArgumentNullException` for invalid name. Returns `null` if not present. |
| `TryGetFact(string name, out Fact? fact)` | Try to get a raw `Fact` | Returns `true` if found; `fact` contains the stored `Fact`. |
| `TryGetFact<T>(string name, out Fact<T>? fact)` | Try to get a `Fact<T>` | If stored fact is `Fact<T>` returns it; if stored fact is non-generic but its value is assignable to `T`, returns a typed copy. |
| `ToDictionary()` | Shallow copy as `Dictionary<string, object?>` | Useful for logging or serialization; values are the runtime values of facts. |
| `IEnumerable<Fact>` | Enumeration over stored facts | Fact equality is based on name. Iterating does not guarantee snapshot isolation. |
| `Remove(string name)` / `Remove(Fact)` / `Remove<T>(Fact<T>)` | Remove facts by name or instance | Removing a non-existing fact is a no-op. `Remove(string)` throws on invalid (null/empty) name. |
| `Clear()` | Remove all facts | Empties the collection. |

Notes:
- Fact name matching is case-sensitive (ordinal comparison).
- Mutating `Facts` from actions affects subsequent rule evaluations in the same engine run unless the engine snapshots facts explicitly.
- Prefer `TryGetValue<T>` in conditions to avoid invalid cast exceptions.

