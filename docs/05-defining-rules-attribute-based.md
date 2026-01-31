Attribute-based rules (source-generator)

Overview

This project supports an attribute-based approach to define rules: write simple POCO classes annotated with provided attributes (`[Rule]`, `[Condition]`, `[Action]`, `[Fact]`, `[Priority]`) and let the source generator produce high-performance adapters at compile time.

- Why use this approach?
  - Developer ergonomics: write plain classes with attributes and avoid boilerplate adapter code.
  - Performance & AOT-friendliness: generated adapters call rule methods directly without runtime reflection, making them efficient and trim-friendly.

How it works (high-level)

1. Author a POCO rule class and annotate methods/properties with attributes (see examples below).
2. The source generator (`LightRules.Generator`) runs at compile time and emits a `{RuleType}_RuleAdapter.g.cs` class that implements `IRule`.
3. The generator also emits a `LightRules.Generated.RuleRegistry` containing `RuleMetadata` entries for all generated adapters.
4. At runtime, the engine uses the generated adapters and the registry; there is no reflection or proxy logic used.

Attributes

- `[Rule(Name = "MyRule", Description = "...")]` — mark a class as a rule. `Name` and `Description` are optional.
- `[Condition]` — annotate a single method that returns `bool` (the rule condition).
- `[Action(Order = 1)]` — annotate one or more methods to execute when the condition is true. Methods may either:
  - Return `Facts` (functional style): the returned `Facts` instance becomes the current facts for subsequent rules; or
  - Be `void` methods (legacy): the generator supports these for compatibility and the adapter will invoke them — prefer returning `Facts` for new code.
- `[Fact("factName")]` — annotate a method parameter to indicate which fact name to inject; if omitted, the parameter name is used.
- `[Priority]` — optional method returning `int` to compute priority dynamically.

Example POCO rule (functional style)

```csharp
using LightRules.Attributes;

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

Generator compatibility note

- The generator recognizes both legacy `void` actions and the new `Facts`-returning actions. When you have legacy `void` methods, the adapter will call them (and wrappers may run them against a mutable builder internally) so existing POCOs remain functional. Prefer returning `Facts` for clarity and immutability.

What the generator emits (conceptual)

- `HighValueOrderRule_RuleAdapter : IRule` with direct calls:
  - `bool Evaluate(Facts facts)` binds `total` using `facts.TryGetValue<decimal>("orderTotal", out var total)` and calls `_target.IsHighValue(total)`.
  - `Facts Execute(Facts facts)` binds parameters, calls action methods in order, and returns the final `Facts` instance.
  - `string Name { get; }`, `string Description { get; }`, `int Priority { get; }` and other metadata.

Discovery and registration

- The generator also produces a registry `LightRules.Generated.RuleRegistry.All` (an array of `RuleMetadata`).
- Use `RuleDiscovery.Discover()` to get `RuleMetadata` entries and register the generated adapters (the engine code expects `IRule` instances).

Adapter constructor note

Generated adapters are normal C# types. Their constructors may vary depending on generator behavior (some adapters expose a parameterless constructor, others accept the original POCO instance). A practical injector should attempt to instantiate an adapter using the original POCO (if available) and fall back to a parameterless constructor. Example strategy:

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

Migration notes

- The codebase no longer uses runtime reflection or dynamic proxies for attribute-based rules. Make sure your POCO rule classes are compiled in the same project (or in a project that has the source generator enabled) so the adapter will be generated.
- If you previously relied on runtime discovery of attribute classes in external assemblies, ensure those assemblies are also built with the generator or that adapters are generated ahead-of-time and included.

Tips & caveats

- The generator emits warnings for unsupported parameter shapes. Keep rule method signatures simple: parameters should either be annotated with `[Fact]` or be a single `Facts` parameter.
- Generated adapters are ordinary C# code, you can view them in your IDE under the generated sources node or in build artifacts.

Next steps / Extensions

- You can extend the generator to support optional facts, default values, or more advanced binding.
- To validate AOT/trimming, add a CI job that builds the solution with `PublishTrimmed`/NativeAOT and runs smoke tests.

```text
# Quick try:
# 1. Add a POCO rule class annotated per above in the project.
# 2. dotnet build (generator will produce adapters)
# 3. Use RuleDiscovery.Discover() to get metadata and register rules.
```
