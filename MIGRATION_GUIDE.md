# Breaking Changes: Facts API Immutability Enforcement

## Summary

This document describes the breaking changes made to enforce immutability and type safety in the `Facts` API.

## Changes Made

### 1. Facts Type Changed from Class to Record
- **Before**: `public sealed class Facts`
- **After**: `public sealed record Facts`
- **Reason**: Records provide value semantics and clearly express immutable intent

### 2. Removed Unsafe Read Methods

#### Removed: Indexer
```csharp
// REMOVED - unsafe
public object? this[string name] => GetFactValue<object>(name);
```
**Why**: Indexers can throw exceptions on type mismatch and don't support safe type checking

#### Removed: GetFactValue<T>
```csharp
// REMOVED - unsafe
public T? GetFactValue<T>(string factName)
```
**Why**: Returns `default(T)` if not present, throws on type mismatch. Not safe for conditions.

#### Removed: GetFact
```csharp
// REMOVED - unsafe
public Fact? GetFact(string factName)
```
**Why**: Returns null if not present. Prefer `TryGetFact` for safe access.

#### Removed: Clone()
```csharp
// REMOVED - disallowed in records
public Facts Clone() => this;
```
**Why**: Records disallow explicit `Clone()` methods. Since `Facts` is immutable, cloning is unnecessary—just use the instance directly.

### 3. Safe Methods Retained

All safe `Try*` methods are retained and are now the **only** way to read facts:

- ✅ `TryGetFactValue<T>(string name, out T? value)` — Safe typed read
- ✅ `TryGetFact(string name, out Fact? fact)` — Safe fact retrieval
- ✅ `TryGetFact<T>(string name, out Fact<T>? fact)` — Safe typed fact retrieval
- ✅ `ContainsFact(string name)` — Check existence

### 4. Engine Changes

Removed all `Clone()` calls since `Facts` is immutable:

**Before**:
```csharp
evaluationResult = rule.Evaluate(currentFacts.Clone());
```

**After**:
```csharp
evaluationResult = rule.Evaluate(currentFacts);  // Safe - Facts is immutable
```

### 5. Documentation Updates

Updated all documentation files to:
- Remove references to unsafe methods (`GetFactValue`, `GetFact`, indexer, `Clone`)
- Show only `TryGet*` method usage in examples
- Clarify that `Facts` is an immutable record
- Update migration guidance for users

**Files updated**:
- `docs/01-defining-facts.md`
- `docs/02-defining-conditions.md`
- `docs/03-defining-actions.md`
- `docs/04-defining-rules.md`
- `docs/08-defining-rules-listener.md`
- `docs/09-defining-rules-engine-listener.md`
- `docs/10-facts-api.md`

### 6. Added Copilot Instructions

Created `.github/copilot-instructions.md` to enforce:
- Always update documentation with code changes
- Only use safe `TryGet*` methods
- Never add unsafe methods back
- Maintain immutability and type safety

## Migration Guide

### For Condition Code

**Before**:
```csharp
public bool Evaluate(Facts facts)
{
    int qty = facts.GetFactValue<int>("quantity");  // Unsafe - throws on mismatch
    return qty > 0;
}
```

**After**:
```csharp
public bool Evaluate(Facts facts)
{
    return facts.TryGetFactValue<int>("quantity", out var qty) && qty > 0;
}
```

### For Action Code

**Before**:
```csharp
public Facts Execute(Facts facts)
{
    var orderId = facts.GetFactValue<string>("orderId");  // Unsafe
    ProcessOrder(orderId);
    return facts.AddOrReplaceFact("processed", true);
}
```

**After**:
```csharp
public Facts Execute(Facts facts)
{
    if (!facts.TryGetFactValue<string>("orderId", out var orderId))
    {
        return facts;  // Gracefully handle missing fact
    }
    
    ProcessOrder(orderId);
    return facts.AddOrReplaceFact("processed", true);
}
```

### For Engine/Listener Code

**Before**:
```csharp
// Clone was needed (incorrectly) to "protect" facts
var snapshot = facts.Clone();
rule.Evaluate(snapshot);
```

**After**:
```csharp
// Facts is immutable, safe to pass directly
rule.Evaluate(facts);
```

## Benefits

1. **Type Safety**: All fact reads are explicitly checked at the call site
2. **No Silent Failures**: `TryGet*` pattern forces handling of missing/mismatched facts
3. **Thread Safety**: Immutable records are inherently thread-safe
4. **Clear Intent**: Code that reads facts is explicit about error handling
5. **Better Testing**: Safer patterns lead to more reliable test code

## Checklist for Future Changes

When modifying the `Facts` API:
- [ ] Never add methods that throw on type mismatch
- [ ] Never add indexers
- [ ] Always provide `Try*` variants for safe access
- [ ] Update all documentation with examples
- [ ] Grep for old method names in docs and samples
- [ ] Run full build and check for errors
- [ ] Update `.github/copilot-instructions.md` if design principles change

## Questions?

See `.github/copilot-instructions.md` for ongoing guidance on maintaining immutability and type safety.
