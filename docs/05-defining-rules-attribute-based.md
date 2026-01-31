# Attribute-based Rules (Source Generator)

<!-- Table of contents -->
- [Overview](#overview)
- [How it works](#how-it-works)
- [Attributes](#attributes)
- [Example](#example)
- [What the generator emits](#what-the-generator-emits)
- [Discovery and registration](#discovery-and-registration)
- [Adapter constructor](#adapter-constructor)
- [Tips and caveats](#tips-and-caveats)

## Overview

This project supports an attribute-based approach to define rules: write simple POCO classes annotated with provided attributes (`[Rule]`, `[Condition]`, `[Action]`, `[Fact]`, `[Priority]`) and let the source generator produce high-performance adapters at compile time.

**Why use this approach?**

- **Developer ergonomics**: write plain classes with attributes and avoid boilerplate adapter code.
- **Performance & AOT-friendliness**: generated adapters call rule methods directly without runtime reflection, making them efficient and trim-friendly.

## How it works

1. Author a POCO rule class and annotate methods/properties with attributes (see examples below).
2. The source generator (`LightRules.Generator`) runs at compile time and emits a `{RuleType}_RuleAdapter.g.cs` class that implements `IRule`.
3. The generator also emits a `LightRules.Generated.RuleRegistry` containing `RuleMetadata` entries for all generated adapters.
4. At runtime, the engine uses the generated adapters and the registry; there is no reflection or proxy logic used.

## Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[Rule(Name = "...", Description = "...")]` | Class | Mark a class as a rule. `Name` and `Description` are optional. |
| `[Condition]` | Method | Annotate a single method that returns `bool` (the rule condition). |
| `[Action(Order = 1)]` | Method | Annotate one or more methods to execute when the condition is true. Methods should return `Facts`. |
| `[Fact("factName")]` | Parameter | Indicate which fact name to inject; if omitted, the parameter name is used. |
| `[Priority]` | Method | Optional method returning `int` to compute priority dynamically. |

## Example

```csharp
using LightRules.Attributes;
using LightRules.Core;

[Rule(Name = "HighValueOrder", Description = "Apply discount to high value orders")]
public class HighValueOrderRule
{
    [Condition]
    public bool IsHighValue([Fact("orderTotal")] decimal total) => total >= 1000m;

    [Action(Order = 1)]
    public Facts ApplyDiscount([Fact("orderId")] string id, Facts facts)
    {
        // return a new Facts instance with the discount flag set
        return facts.Set("discountApplied", true);
    }
}
```

## What the generator emits

For the example above, the generator produces `HighValueOrderRule_RuleAdapter : IRule` with:

- `bool Evaluate(Facts facts)` — binds `total` using `facts.TryGetValue<decimal>("orderTotal", out var total)` and calls `_target.IsHighValue(total)`.
- `Facts Execute(Facts facts)` — binds parameters, calls action methods in order, and returns the final `Facts` instance.
- `string Name { get; }`, `string Description { get; }`, `int Priority { get; }` — metadata properties.

## Discovery and registration

The generator produces a registry `LightRules.Generated.RuleRegistry.All` (an array of `RuleMetadata`).

Use `RuleDiscovery.Discover()` to get `RuleMetadata` entries and register the generated adapters:

```csharp
var metas = RuleDiscovery.Discover();
var rules = new Rules();

foreach (var meta in metas)
{
    var adapter = (IRule)Activator.CreateInstance(meta.RuleType)!;
    rules.Register(adapter);
}

var engine = new DefaultRulesEngine();
var finalFacts = engine.Fire(rules, facts);
```

## Adapter constructor

Generated adapters are normal C# types. Their constructors may vary depending on generator behavior (some adapters expose a parameterless constructor, others accept the original POCO instance).

A practical injector should attempt to instantiate an adapter using the original POCO (if available) and fall back to a parameterless constructor:

```csharp
IRule CreateAdapter(Type adapterType, object? poco = null)
{
    try
    {
        if (poco != null)
        {
            return (IRule)Activator.CreateInstance(adapterType, poco)!;
        }
    }
    catch { }
    return (IRule)Activator.CreateInstance(adapterType)!;
}
```

## Tips and caveats

- The generator emits warnings for unsupported parameter shapes. Keep rule method signatures simple: parameters should either be annotated with `[Fact]` or be a single `Facts` parameter.
- Generated adapters are ordinary C# code — you can view them in your IDE under the generated sources node or in build artifacts.
- You can extend the generator to support optional facts, default values, or more advanced binding.
- To validate AOT/trimming, add a CI job that builds the solution with `PublishTrimmed`/NativeAOT and runs smoke tests.

### Quick start

```bash
# 1. Add a POCO rule class annotated per above in the project
# 2. Build the project (generator will produce adapters)
dotnet build

# 3. Use RuleDiscovery.Discover() to get metadata and register rules
```
