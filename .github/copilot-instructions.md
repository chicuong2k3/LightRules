# GitHub Copilot Instructions for LightRules

## Project Overview
LightRules is a lightweight, immutable, and type-safe rules engine for .NET. It emphasizes:
- **Immutability**: `Facts` is an immutable record; all mutations return new instances
- **Type safety**: Only `TryGet*` methods are available for reading facts
- **Functional style**: Actions return new `Facts` instances
- **Source generation**: Attribute-based rules use compile-time generation (no reflection)

## Core Design Principles

### 1. Facts Are Immutable Records
- `Facts` is a sealed record backed by `ImmutableDictionary`
- All mutation methods (`AddOrReplaceFact`, `AddFact`, `RemoveFact`) return new `Facts` instances
- **Never** expose unsafe methods like `GetFactValue<T>` (throws on type mismatch) or indexers
- **Only** expose `TryGetFactValue<T>`, `TryGetFact`, `TryGetFact<T>` for reading
- `Clone()` method is disallowed in records — Facts is already immutable

### 2. Actions Return New Facts
- `IAction.Execute(Facts facts)` signature: `Facts Execute(Facts facts)`
- Actions must return a `Facts` instance (possibly modified)
- The engine threads `Facts` through rules: each rule's action output becomes input to the next
- Example:
  ```csharp
  public Facts Execute(Facts facts)
  {
      return facts.AddOrReplaceFact("processed", true);
  }
  ```

### 3. Conditions Are Pure Predicates
- `ICondition.Evaluate(Facts facts)` should be side-effect free
- Only read facts using `TryGetFactValue<T>`
- Never mutate facts inside conditions (immutability makes this impossible anyway)
- Example:
  ```csharp
  public bool Evaluate(Facts facts)
  {
      return facts.TryGetFactValue<int>("quantity", out var q) && q > 0;
  }
  ```

### 4. Type Safety Enforcement
- **Always** use `TryGetFactValue<T>` or `TryGetFact<T>` to read facts
- **Never** introduce unsafe methods like `GetFactValue<T>` (returns default/throws on mismatch)
- **Never** introduce indexers on `Facts`
- If a fact is missing or type doesn't match, handle gracefully with `TryGet*` pattern

## Documentation Standards

### Always Update Documentation With Code Changes
When modifying code:
1. **Identify affected docs**: Search for related keywords in `docs/*.md`
2. **Update all references**: Ensure examples, API tables, and explanations match implementation
3. **Check samples**: Update `samples/` code if APIs change
4. **Validate consistency**: Run `grep` to find stale references to removed methods

### Documentation Structure
- `docs/01-defining-facts.md` — Facts collection basics and API
- `docs/02-defining-conditions.md` — Condition interface and semantics
- `docs/03-defining-actions.md` — Action interface and best practices
- `docs/04-defining-rules.md` — Rule definition (attribute-based, programmatic, fluent)
- `docs/05-defining-rules-attribute-based.md` — Source generator details
- `docs/06-defining-engine.md` — Engine parameters and configuration
- `docs/07-defining-rules-engine.md` — Engine implementations
- `docs/08-defining-rules-listener.md` — Per-rule lifecycle listeners
- `docs/09-defining-rules-engine-listener.md` — Engine-level listeners
- `docs/10-facts-api.md` — Quick Facts API reference table
- `docs/FLUENT_API.md` — Fluent builder API guide
- `docs/FEATURES_AND_ROADMAP.md` — Feature list and roadmap

### Example Patterns in Documentation

#### ✅ Good (Safe, Immutable)
```csharp
// Reading facts
if (facts.TryGetFactValue<int>("quantity", out var qty))
{
    Console.WriteLine($"Quantity: {qty}");
}

// Modifying facts in actions
public Facts Execute(Facts facts)
{
    return facts.AddOrReplaceFact("orderAccepted", true);
}

// Chaining mutations
facts = facts
    .AddOrReplaceFact("processed", true)
    .AddOrReplaceFact("timestamp", DateTime.UtcNow);
```

#### ❌ Bad (Unsafe, Mutable)
```csharp
// NEVER use these patterns:
var qty = facts.GetFactValue<int>("quantity");  // Method removed - unsafe
var name = facts["name"];  // Indexer removed - unsafe
facts.Clone();  // Disallowed in records

// NEVER mutate in conditions:
public bool Evaluate(Facts facts)
{
    facts = facts.AddOrReplaceFact("evaluated", true);  // Wrong - conditions are read-only
    return true;
}
```

## Code Review Checklist
When reviewing code changes:
- [ ] Facts is still immutable (no in-place mutations)
- [ ] Only `TryGet*` methods used for reading facts
- [ ] Actions return `Facts` instances
- [ ] Conditions are pure (read-only, no side effects)
- [ ] Documentation updated to match code changes
- [ ] Examples in docs use safe patterns
- [ ] Sample code compiles and runs
- [ ] No references to removed methods (`GetFactValue`, `GetFact`, indexer, `Clone`)

## Common Mistakes to Avoid
1. **Adding unsafe read methods**: Don't add `GetFactValue<T>` or indexers back
2. **Forgetting to update docs**: Every API change must update docs
3. **Using mutable patterns**: Don't assume Facts can be modified in-place
4. **Stale examples**: Grep for old method names when removing APIs
5. **Reference equality assumptions**: Facts is a record with value semantics

## Testing Guidelines
- Test that mutations return new instances
- Test that conditions don't modify facts
- Test that actions properly chain facts through execution
- Test thread safety (multiple threads reading same Facts instance)
- Test type mismatches are handled gracefully with `TryGet*`

## Commit Message Guidelines
- **feat**: New feature (e.g., "feat: add fluent condition builders")
- **fix**: Bug fix (e.g., "fix: correct Facts equality semantics")
- **docs**: Documentation only (e.g., "docs: update Facts API reference")
- **refactor**: Code change without behavior change (e.g., "refactor: simplify rule evaluation logic")
- **test**: Test changes (e.g., "test: add immutability tests for Facts")
- **chore**: Build/tooling changes (e.g., "chore: update source generator NuGet package")

Always include scope when relevant: `feat(facts): ...`, `docs(conditions): ...`

## Questions?
If uncertain about a design decision:
1. Check existing patterns in the codebase
2. Review this instruction file
3. Search docs for similar use cases
4. Ensure immutability and type safety are preserved
