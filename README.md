# LightRules

[![Build Status](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions)
[![Coverage Status](https://img.shields.io/coverage/placeholder.svg)](https://coverage.example.com)
[![NuGet Version](https://img.shields.io/nuget/v/LightRules.svg)](https://www.nuget.org/packages/LightRules)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

LightRules is a lightweight .NET rules engine for defining, evaluating and executing business rules against a set of facts.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Project Status](#project-status)
- [Prerequisites](#prerequisites)
- [Build & Install](#build--install)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Roadmap](#roadmap)
- [Samples](#samples)
- [Contributing](#contributing)
  - [Contribution Rules](#contribution-rules)
- [License](#license)
- [Contact](#contact)

---

## Overview

`LightRules` is a small, focused rules engine for .NET that supports two primary ways of authoring rules:

1. Programmatic: implement the `IRule` interface or use helper classes such as `DefaultRule` and `BasicRule`.
2. Attribute-based (recommended): annotate plain POCO classes with `[Rule]`, `[Condition]`, `[Action]`, and `[Fact]` and let a Roslyn source generator (`LightRules.Generator`) emit efficient `IRule` adapters at compile-time.

The attribute-based approach avoids runtime reflection and is suitable for AOT/trimming scenarios.

## Key Features

- Minimal runtime API: `IRule`, `Facts`, `Rules`, `IRulesEngine`.
- Attribute-based source generator that produces efficient adapters (no MethodInfo.Invoke at runtime).
- Two engine implementations: `DefaultRulesEngine` (sequential) and `InferenceRulesEngine` (iterative inference).
- Listeners: `IRuleListener` (per-rule) and `IRulesEngineListener` (engine-level) for metrics, logging and control.
- Simple discovery helper: `RuleDiscovery.Discover()` to enumerate generated adapters.

## Project Status

- CI: placeholder badge above — wire your CI workflow at `.github/workflows/ci.yml` to enable the build badge.
- Coverage: placeholder coverage badge — configure coverage reporting (Coverlet / Codecov / other) to enable.
- NuGet: package publishing is planned; current version badge is a placeholder.

Status summary:
- Core runtime: implemented (IRule, Facts, Rules, DefaultRulesEngine, InferenceRulesEngine).
- Attribute generator: implemented in `LightRules.Generator` (confirm in your local build).
- Documentation: core docs present under `docs/` and ordered for guided reading.
- Samples: minimal sample available under `samples/` (you can extend with more scenarios).

If you'd like, I can set up a GitHub Actions CI pipeline and a coverage report configuration; tell me your preferred CI and coverage provider.

## Prerequisites

- .NET SDK (6.0, 7.0, or later depending on project targets).
- A C# build environment that runs Roslyn source generators (standard `dotnet build` is sufficient when the generator is referenced).

## Build & Install

From the repository root run:

```bash
dotnet build LightRules.sln
```

This builds the runtime project and runs the source generator (if configured for your project) to produce adapters for attribute-based rules.

## Quick Start (updated for immutable Facts)

1. Create or discover rules:
   - Programmatic: implement `IRule` (note: `Execute` now returns `Facts`) or use `DefaultRule`.
   - Attribute-based: annotate POCOs and build (the source generator will emit adapters). The generator supports legacy void actions for compatibility.

2. Create and populate an immutable `Facts` instance (use fluent `AddOrReplaceFact` to return a new instance):

```csharp
var facts = new Facts();
facts = facts.AddOrReplaceFact("quantity", 5);
```

3. Build and run the engine:

```csharp
var rules = new Rules(/* discovered or programmatic */);
var engine = new DefaultRulesEngine();
var finalFacts = engine.Fire(rules, facts);
```

4. Example: attribute-based discovery and instantiation (no reflection required):

```csharp
// Rules are auto-registered via ModuleInitializer when assembly loads
var metas = RuleDiscovery.Discover();
var rules = new Rules();

foreach (var meta in metas)
{
    // Use factory to create instance - no Activator.CreateInstance needed!
    var instance = meta.CreateInstance();
    rules.Register(instance);
}

var engine = new DefaultRulesEngine();
var finalFacts = engine.Fire(rules, facts);
```

> Note: LightRules is fully AOT-compatible. Discovery, registration, and instantiation all work without runtime reflection.

## Documentation

The repository includes ordered documentation files under `docs/`. Prefer reading them in the following order for a linear learning path:

1. `docs/01-defining-facts.md` — Facts API and examples.
2. `docs/02-defining-conditions.md` — Writing conditions.
3. `docs/03-defining-actions.md` — Writing actions.
4. `docs/04-defining-rules.md` — Rules overview and programmatic approach.
5. `docs/05-defining-rules-attribute-based.md` — Attribute-based rules and source generator notes.
6. `docs/06-defining-engine.md` — Engine parameters and listeners (compact).
7. `docs/07-defining-rules-engine.md` — Engine usage and examples (expanded).
8. `docs/08-defining-rules-listener.md` — Per-rule listeners.
9. `docs/09-defining-rules-engine-listener.md` — Engine-level listeners.

## Samples

Sample projects live under `samples/`. To run the provided sample app (if present):

```bash
dotnet run --project samples/SampleApp/SampleApp.csproj
```

### Run the sample (one-command)

Build the entire solution and run the SampleApp with a single command. This is the recommended quick path for beginners — it will compile the generator, produce adapters, and execute the sample.

```bash
# One-line: build the solution and run the sample app
dotnet build LightRules.sln && dotnet run --project samples/SampleApp/SampleApp.csproj
```

The sample demonstrates a POCO rule annotated with attributes (see `samples/SampleApp/HighValueOrderRule.cs`) and a tiny runner that:

- Discovers generated adapters via `RuleDiscovery.Discover()`
- Instantiates adapters (tries POCO ctor, falls back to parameterless)
- Fires the `DefaultRulesEngine` with a `Facts` bag and prints facts before/after

For more details, see `samples/SampleApp/README.md`.

## Contributing

Contributions are welcome. Suggested workflow:

1. Fork the repository.
2. Create a feature branch for your change.
3. Add tests where appropriate.
4. Open a pull request describing the change.

### Contribution Rules

To keep the project healthy and reviewable, please follow these rules when contributing:

- Small focused PRs: one logical change per PR.
- Code style: follow standard C# conventions (use the existing project style). Run code formatters before committing.
- Tests: include unit tests for runtime behavior changes and generator changes. New features should have tests demonstrating the happy path.
- Documentation: update `docs/` and `README.md` when you change public behavior or add features.
- Commit messages: use clear, imperative messages. Prefer `chore:`, `feat:`, `fix:`, `docs:`, `test:` prefixes.
- Review process: maintainers will review PRs, request changes, and merge when green. Be responsive to review comments.
- Breaking changes: signal breaking API changes in the PR title and description and update the roadmap and CHANGELOG accordingly.

If you want an automated pre-commit or CI checks (formatting, linting, tests), I can add a suggested GitHub Actions workflow and pre-commit configuration.

## License

This project is licensed under the MIT License. See the `LICENSE` file in the repository root for details.

## Contact

Open an issue on GitHub for bugs, questions, or feature requests.
