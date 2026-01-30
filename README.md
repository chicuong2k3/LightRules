# LightRules

LightRules is a small, focused .NET rules engine. It provides lightweight abstractions for defining rules, evaluating them against a set of facts, and executing actions when rules fire.

Key features
- Small runtime API (`IRule`, `Facts`, `Rules`, `RulesEngine`).
- Two ways to define rules:
  - Programmatic: implement `IRule` or use helper classes like `DefaultRule`.
  - Attribute-based: write POCO classes annotated with `[Rule]`, `[Condition]`, `[Action]`, `[Fact]`, and let the source generator produce adapters at compile time (recommended).
- Source-generator-based adapters:
  - No runtime reflection or dynamic proxies.
  - AOT-friendly and efficient: generated adapters call rule methods directly and bind facts using typed accessors.
- Extensible engine with listeners and engine parameters.

Documentation
- `docs/defining-rules.md` — Overview of rules and the attribute-based approach.
- `docs/defining-rules-attribute-based.md` — Full guide for attribute-based rules and generator details.
- `docs/defining-facts.md` — Facts API and examples.
- `docs/defining-conditions.md` — Writing conditions.
- `docs/defining-actions.md` — Writing actions.
- `docs/defining-rules-engine.md` — Engine usage and parameters.
- `docs/defining-rules-engine-listener.md` — Engine-level listeners.
- `docs/defining-rules-listener.md` — Per-rule listeners

Build and run

This repository contains two projects:
- `LightRules` (the runtime library)
- `LightRules.Generator` (the Roslyn source generator that emits adapters)

To build the solution and generate adapters:

```bash
dotnet build LightRules.sln
```

If you want to experiment with attribute-based rules, add a POCO rule class (annotated with `[Rule]`, etc.) to the `LightRules` project or to a project that references the generator.

Example quick run (create a small console that references `LightRules`):

1. Create a new console app in the solution or a sibling project.
2. Add a POCO rule class in that project and ensure the project references the `LightRules.Generator` analyzer (the sample solution already wires the generator for `LightRules`).
3. Build the solution: `dotnet build` — generated adapters appear as part of compilation.
4. Use `RuleDiscovery.Discover()` and `DefaultRulesEngine` to run rules with a `Facts` instance.

Samples and tests
- Add sample projects under `samples/` to demonstrate attribute-based rules and engine usage (recommended). If you want, I can scaffold a sample for you.

Contributing & notes
- The generator-only approach is the recommended path to build attribute-based rules that are AOT-friendly.
- If you add rules in a separately compiled assembly, that assembly must also include the generator (or the adapter must be generated and committed) so that adapters exist at runtime.

License
- This project is MIT-licensed (check the repository root for LICENSE file if present).
