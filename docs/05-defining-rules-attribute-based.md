# Attribute-based Rules (Source Generator)

<!-- Table of contents -->
- [Overview](#overview)
- [How it works](#how-it-works)
- [Attributes](#attributes)
- [Example](#example)
- [What the generator emits](#what-the-generator-emits)
- [Discovery and registration](#discovery-and-registration)
- [AOT and trimming support](#aot-and-trimming-support)
- [Tips and caveats](#tips-and-caveats)

## Overview

This project supports an attribute-based approach to define rules: write simple POCO classes annotated with provided attributes (`[Rule]`, `[Condition]`, `[Action]`, `[Fact]`, `[Priority]`) and let the source generator produce high-performance adapters at compile time.

**Why use this approach?**

- **Developer ergonomics**: write plain classes with attributes and avoid boilerplate adapter code.
- **Performance & AOT-friendliness**: generated adapters call rule methods directly without runtime reflection, making them efficient and trim-friendly.
- **No reflection required**: discovery, registration, and instance creation all work without reflection.

## How it works

1. Author a POCO rule class and annotate methods/properties with attributes (see examples below).
2. The source generator (`LightRules.Generator`) runs at compile time and emits a `{RuleType}_RuleAdapter.g.cs` class that implements `IRule`.
3. The generator also emits a `ModuleInitializer` that auto-registers rules into `GlobalRuleRegistry` when the assembly loads.
4. At runtime, use `RuleDiscovery.Discover()` to get all registered rules and `meta.CreateInstance()` to create instances - no reflection needed.

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
        Console.WriteLine($"Applying discount to order {id}");
        return facts.AddOrReplaceFact("discountApplied", true);
    }
}
```

## What the generator emits

For the example above, the generator produces `HighValueOrderRule_RuleAdapter : IRule` with:

- `bool Evaluate(Facts facts)` — binds `total` using `facts.TryGetFactValue<decimal>("orderTotal", out var total)` and calls `_target.IsHighValue(total)`.
- `Facts Execute(Facts facts)` — binds parameters, calls action methods in order, and returns the final `Facts` instance.
- `string Name { get; }`, `string Description { get; }`, `int Priority { get; }` — metadata properties.

The generator also emits a `ModuleInitializer` that auto-registers rules when the assembly loads.

## Discovery and registration

Rules are automatically registered via `ModuleInitializer` when the assembly loads. Use `RuleDiscovery.Discover()` to access them:

```csharp
// Get all discovered rules (auto-registered via ModuleInitializer)
var metas = RuleDiscovery.Discover();
var rules = new Rules();

foreach (var meta in metas)
{
    // Create instance using factory - no reflection required!
    var ruleInstance = meta.CreateInstance();
    rules.Register(ruleInstance);
}

var engine = new DefaultRulesEngine();
var finalFacts = engine.Fire(rules, facts);
```

Key points:
- **No reflection**: `RuleDiscovery.Discover()` returns rules from `GlobalRuleRegistry` (populated by `ModuleInitializer`).
- **Factory pattern**: `meta.CreateInstance()` uses a compile-time generated factory lambda, not `Activator.CreateInstance`.
- **AOT-friendly**: The entire discovery and instantiation process works without runtime reflection.

## AOT and trimming support

LightRules is designed to work with AOT compilation and trimming:

- **No reflection for discovery**: `ModuleInitializer` auto-registers rules at assembly load time.
- **No reflection for instantiation**: `RuleMetadata.CreateInstance()` uses a factory lambda generated at compile time.
- **No dynamic type loading**: All types are known at compile time.

To validate AOT/trimming compatibility:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishTrimmed=true
```

## Tips and caveats

- The generator emits warnings for unsupported parameter shapes. Keep rule method signatures simple: parameters should either be annotated with `[Fact]` or be a single `Facts` parameter.
- Generated adapters are ordinary C# code — you can view them in your IDE under the generated sources node or in build artifacts.
- Rules are automatically discovered when their assembly loads; no manual registration is needed.
- For unit testing, you can use `GlobalRuleRegistry.Clear()` to reset the registry between tests.

### Quick start

```bash
# 1. Add a POCO rule class annotated per above in the project
# 2. Build the project (generator will produce adapters)
dotnet build

# 3. Use RuleDiscovery.Discover() and CreateInstance() to run rules
var rules = new Rules();
foreach (var meta in RuleDiscovery.Discover())
{
    rules.Register(meta.CreateInstance());
}
```
