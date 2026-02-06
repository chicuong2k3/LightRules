# Facts API Quick Reference Card

## 🎯 Core Principle: Type Safety First

`Facts` is an **immutable record**. Use only **TryGet*** methods for reading.

---

## ✅ Safe Reading Patterns (USE THESE)

### Read a typed value
```csharp
if (facts.TryGetFactValue<int>("quantity", out var qty))
{
    Console.WriteLine($"Quantity: {qty}");
}
else
{
    // Handle missing or wrong type
}
```

### Read a raw Fact
```csharp
if (facts.TryGetFact("customer", out var fact))
{
    Console.WriteLine($"Customer: {fact.Value}");
}
```

### Read a typed Fact<T>
```csharp
if (facts.TryGetFact<string>("customer", out var typedFact))
{
    Console.WriteLine($"Customer: {typedFact.Value}");
}
```

### Check existence
```csharp
if (facts.ContainsFact("orderId"))
{
    // Fact exists
}
```

---

## ✅ Writing/Mutating Facts (Returns New Instance)

### Add or replace a fact
```csharp
facts = facts.AddOrReplaceFact("quantity", 10);
```

### Add a Fact instance
```csharp
facts = facts.AddFact(new Fact("customer", "Alice"));
facts = facts.AddFact(new Fact<string>("email", "alice@example.com"));
```

### Remove a fact
```csharp
facts = facts.RemoveFact("oldData");
```

### Chain mutations
```csharp
facts = facts
    .AddOrReplaceFact("processed", true)
    .AddOrReplaceFact("timestamp", DateTime.UtcNow)
    .RemoveFact("tempData");
```

---

## ✅ Conditions (Pure Predicates)

```csharp
public bool Evaluate(Facts facts)
{
    // Use TryGetFactValue for safe access
    return facts.TryGetFactValue<int>("age", out var age) && age >= 18;
}
```

**Rules**:
- Read-only (Facts is immutable anyway)
- Use TryGetFactValue<T>
- Return boolean
- No side effects

---

## ✅ Actions (Return New Facts)

```csharp
public Facts Execute(Facts facts)
{
    // Check fact exists and get value
    if (!facts.TryGetFactValue<string>("orderId", out var orderId))
    {
        return facts; // Gracefully handle missing fact
    }
    
    // Process and return new Facts
    ProcessOrder(orderId);
    return facts.AddOrReplaceFact("orderProcessed", true);
}
```

**Rules**:
- Accept Facts parameter
- Return Facts instance (possibly modified)
- Use TryGetFactValue<T> for reading
- Use AddOrReplaceFact for writing
- Handle missing facts gracefully

---

## ❌ Removed Methods (DO NOT USE)

```csharp
// ❌ REMOVED - Indexer
var value = facts["name"];

// ❌ REMOVED - GetFactValue
int qty = facts.GetFactValue<int>("quantity");

// ❌ REMOVED - GetFact
Fact? fact = facts.GetFact("name");

// ❌ REMOVED - Clone
Facts copy = facts.Clone();
```

**Why removed?**
- Unsafe type casting (can throw)
- Silent failures (returns null/default)
- Not needed (Facts is immutable)

---

## 🔄 Migration Cheat Sheet

| Old (Unsafe) | New (Safe) |
|---|---|
| `facts["name"]` | `facts.TryGetFactValue<object>("name", out var value)` |
| `facts.GetFactValue<T>("name")` | `facts.TryGetFactValue<T>("name", out var value)` |
| `facts.GetFact("name")` | `facts.TryGetFact("name", out var fact)` |
| `facts.Clone()` | `facts` (just use directly - it's immutable) |

---

## 🎨 Common Patterns

### Pattern: Guard Clause
```csharp
public Facts Execute(Facts facts)
{
    if (!facts.TryGetFactValue<string>("orderId", out var orderId))
        return facts; // Early return if fact missing
    
    // Continue with orderId...
    return facts.AddOrReplaceFact("processed", true);
}
```

### Pattern: Multiple Reads
```csharp
public bool Evaluate(Facts facts)
{
    return facts.TryGetFactValue<int>("quantity", out var qty) && qty > 0
        && facts.TryGetFactValue<bool>("inStock", out var inStock) && inStock;
}
```

### Pattern: Chaining
```csharp
facts = facts
    .AddOrReplaceFact("step1", "complete")
    .AddOrReplaceFact("step2", "complete")
    .AddOrReplaceFact("finalStatus", "success");
```

### Pattern: Conditional Mutation
```csharp
public Facts Execute(Facts facts)
{
    if (facts.TryGetFactValue<bool>("isPremium", out var premium) && premium)
    {
        return facts.AddOrReplaceFact("discount", 0.20m);
    }
    return facts.AddOrReplaceFact("discount", 0.05m);
}
```

---

## 💡 Pro Tips

1. **Always use TryGet*** - No exceptions on type mismatch
2. **Handle missing facts** - Check return value of TryGet*
3. **Chain mutations** - Fluent API for readability
4. **Return new Facts** - Actions must return updated instance
5. **Keep conditions pure** - No mutations in Evaluate()

---

## 📚 Full Documentation

- **API Reference**: `docs/10-facts-api.md`
- **Migration Guide**: `MIGRATION_GUIDE.md`
- **Design Principles**: `.github/copilot-instructions.md`
- **Complete Changes**: `CHANGES_SUMMARY.md`

---

**Remember**: Facts is **immutable** and **type-safe**. Use **TryGet*** for reading! 🎯
