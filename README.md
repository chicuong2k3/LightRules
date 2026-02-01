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
- [Roadmap (short)](#roadmap-short)
- [Prerequisites](#prerequisites)
- [Build & Install](#build--install)
- [Quick Start](#quick-start)
- [Fluent API Example](#fluent-api-example)
- [Documentation](#documentation)
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

A new Fluent API is provided to make programmatic rule creation concise and readable (`RuleBuilder`, `ConditionBuilder`, `ActionBuilder`). See `docs/FLUENT_API.md`.

## Key Features

- Minimal runtime API: `IRule`, `Facts`, `Rules`, `IRulesEngine`.
- Attribute-based source generator that produces efficient adapters (no MethodInfo.Invoke at runtime).
- Two engine implementations: `DefaultRulesEngine` (sequential) and `InferenceRulesEngine` (iterative inference).
- Listeners: `IRuleListener` (per-rule) and `IRulesEngineListener` (engine-level) for metrics, logging and control.
- Simple discovery helper: `RuleDiscovery.Discover()` to enumerate generated adapters.

## Project Status

- CI: badge present; configure `.github/workflows/ci.yml` to enable continuous builds.
- Coverage: placeholder badge — configure coverage reporting to enable.
- Package: NuGet packaging is planned.

Short status:
- Core runtime implemented and tested locally.
- Attribute generator present.
- Fluent builders added for programmatic authoring.

## Roadmap (short)

- Short term: `ISession` scaffold, Agenda API, better tests and CI.
- Mid term: LINQ-based DSL, pattern matcher (RETE or incremental), queries.
- Long term: session persistence, decision tables, CEP features.

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

## Fluent API Example

A compact example using the Fluent API:

```csharp
using LightRules.Core.Fluent;
using LightRules.Core;

var rule = RuleBuilder.Create("OrderPositive")
    .WithDescription("Fires when order quantity is positive")
    .WithPriority(10)
    .When(f => f.TryGetFactValue<int>("quantity", out var q) && q > 0)
    .Then(f => f.AddOrReplaceFact("orderAccepted", true))
    .Build();

var rules = new Rules(rule);
```

## Documentation

The repository includes ordered documentation files under `docs/`. Prefer reading them in the following order for a linear learning path:

1. `docs/01-defining-facts.md` — Facts API and examples.
2. `docs/02-defining-conditions.md` — Writing conditions.
3. `docs/03-defining-actions.md` — Writing actions.
4. `docs/04-defining-rules.md` — Rules overview and programmatic approach.
5. `docs/05-defining-rules-attribute-based.md` — Attribute-based rules and source generator notes.
6. `docs/FLUENT_API.md` — Fluent builder examples and migration notes.
7. `docs/06-defining-engine.md` — Engine parameters and listeners (compact).
8. `docs/07-defining-rules-engine.md` — Engine usage and examples (expanded).
9. `docs/08-defining-rules-listener.md` — Per-rule listeners.
10. `docs/09-defining-rules-engine-listener.md` — Engine-level listeners.

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
