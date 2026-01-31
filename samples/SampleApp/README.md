# SampleApp

This small sample demonstrates an attribute-based rule using `LightRules.Generator` and how to discover and run generated adapters.

How it works

1. `HighValueOrderRule.cs` is a POCO decorated with `Rule`, `Condition`, and `Action` attributes.
2. The `LightRules.Generator` source generator emits an adapter type (e.g. `HighValueOrderRule_RuleAdapter`) and a `LightRules.Generated.RuleRegistry` entry.
3. `Program.cs` calls `RuleDiscovery.Discover()` to get `RuleMetadata`, instantiates adapters (trying POCO ctor first, then parameterless), and executes them via `DefaultRulesEngine`.

Run

```bash
dotnet build LightRules.sln
dotnet run --project samples/SampleApp/SampleApp.csproj
```

If the discovery step prints `Discovered 0 rule metadata entries`, ensure the generator is configured and that `HighValueOrderRule` compiles in the project that references the generator.
