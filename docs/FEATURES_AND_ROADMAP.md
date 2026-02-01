LightRules — Current Feature List and Roadmap
=============================================

This document summarizes the current capabilities of the LightRules project (what's in the repository now) and provides a prioritized, practical roadmap toward feature parity with mature rule engines.

Part A — Current feature list (concrete)
----------------------------------------
These features are implemented in the repository as of this snapshot:

Core runtime and concepts
- Fact model
  - `Fact` (non-generic) and `Fact<T>` (generic) types.
  - `Facts` collection implemented as an immutable container with helpers to create modified copies.
- Condition API
  - `ICondition` interface and `Conditions` helpers (`True`, `False`, `From`).
  - Small combinators: AND/OR/NOT helper (ConditionCombinators).
- Action API
  - `IAction` interface (functional style): `Facts Execute(Facts facts)`.
  - `Actions` helper: `From(Func<Facts, Facts>)` and a compatibility wrapper `From(Action<Facts>)`.
- Rule API
  - `IRule` interface (Evaluate + Execute returning Facts), `BasicRule`, `DefaultRule`.
  - `Rules` collection that orders rules by priority then name.

Engines
- `IRulesEngine` abstraction.
- `DefaultRulesEngine`: sequential evaluator/executor that threads immutable `Facts` forward and supports engine parameters (skip on first applied rule, etc.).
- `InferenceRulesEngine`: present as an iterative engine implementation (for simple inference loops).
- `RulesEngineParameters`: execution hints and thresholds.

Discovery & authoring
- Attribute-based authoring via POCOs and attributes in `LightRules.Attributes`.
- Source-generator: `LightRules.Generator` (Roslyn) produces adapter `IRule` types at compile-time.
- `RuleDiscovery` and generated `RuleRegistry` metadata to enumerate adapters at runtime.
- Generator compatibility: supports both new `Facts`-returning action methods and legacy void actions (migration path).

Instrumentation & integration
- Listeners
  - `IRuleListener` (per-rule lifecycle hooks).
  - `IRulesEngineListener` (engine-level hooks).
- Examples and a SampleApp demonstrating discovery and engine wiring.

Documentation
- docs/ folder with step-by-step docs for facts, conditions, actions, rules, engine parameters, listeners, and attribute-based rules — updated for the immutable API.

Part B — Feature gaps vs NRules and Drools (summary)
---------------------------------------------------
Drools and NRules are mature engines; LightRules deliberately started small and pragmatic. Compared to those engines, the major missing capabilities are:

1) Efficient pattern-matching (RETE or equivalent)
- Drools / NRules: RETE-based pattern network with incremental matching and memory.
- LightRules: sequential engine and a simple inference engine only — no RETE network, no incremental pattern matching.

2) Stateful session / working memory with incremental updates
- Drools/NRules: session semantics (insert/update/retract) and reactive incremental evaluation.
- LightRules: currently one-shot invocation model; immutable Facts are threaded forward — no insert/update/retract session API nor incremental firing.

3) Agenda & activation control
- Drools/NRules: agenda, salience/priorities, activation groups, manual control of firing.
- LightRules: simple priority ordering and engine parameters; no full agenda/activation API.

4) Pattern/query language & DSL
- Drools: DRL, decision tables, rich DSL.
- NRules: LINQ-based rule definition with expressive pattern matching.
- LightRules: attribute + POCO approach via generator; no LINQ DSL, no decision table support, no expressive pattern query engine.

5) Truth maintenance, sessions persistence, distributed execution
- Drools: truth maintenance and session persistence features.
- LightRules: no TMS or persistence of sessions/working memory.

6) Complex event processing (temporal/streaming)
- Drools CEP / Drools Fusion features.
- LightRules: no CEP/time-window support.

7) Tooling & governance
- Rule editors, web consoles, rule repositories (Drools Business Central).
- LightRules: no special tooling beyond code + generator.

Part C — Prioritized roadmap to approach parity with NRules (practical)
------------------------------------------------------------------------
Goal: focus on parity with NRules first (a .NET-focused target) — then consider Drools-level capabilities (DRL, DMN, heavy tooling) as optional long-term goals.

Priority A — Foundational, high-impact (short term, low→medium effort)
1) Add a stateful session API (ISession)
   - Implement `ISession` with methods: `Insert(object fact)`, `Update(object fact)`, `Retract(object fact)`, `Fire()`, `Check()`.
   - Provide an in-memory basic session backed by the existing engine as a first pass (no incremental optimization yet).
   - Tests: insertion -> fire -> update -> fire -> retract -> fire.
   - Why: enables stateful rule usage and is required for most enterprise uses.
   - Effort: low→medium.

2) Add an Agenda / Activation abstraction (skeleton)
   - Define `Activation` objects (rule references + bound facts), an `Agenda` API to inspect scheduled activations, salience, and filtering.
   - Keep the DefaultRulesEngine compatible with the Agenda (initially build the agenda via evaluations each Fire).
   - Why: prepares for advanced conflict resolution & manual control.
   - Effort: medium.

Priority B — Performance & pattern-matching (medium→high effort)
3) Design and implement an efficient pattern-matching engine (RETE or incremental alternative)
   - Evaluate options: implement RETE from scratch, adapt an existing .NET RETE implementation, or design a hybrid incremental evaluation using expression trees and indices.
   - Start with a minimal beta: support simple joins and alpha/beta index structures.
   - Why: required for performance and complex LHS pattern semantics.
   - Effort: high.

4) LINQ-based rule DSL (developer ergonomics)
   - Provide a fluent API that compiles to patterns that feed the match engine (similar ergonomics to NRules).
   - Why: easier to author complex patterns without attribute boilerplate.
   - Effort: medium→high.

Priority C — Usability, tooling and advanced features (lower effort incrementally)
5) Queries API
   - Allow queries over working memory (retrieve matches without firing). Integrate with session and pattern engine.
   - Effort: medium.

6) Session persistence & snapshots
   - Provide optional persistence for sessions (serialize working memory to a store) and session restore.
   - Effort: medium.

7) Decision table support (CSV/Excel) and migration tools
   - Add a loader that converts decision table rows into rules or templates.
   - Effort: medium.

8) CEP/time windows (optional advanced)
   - Add event types, sliding windows, temporal constraints — integrate with match engine.
   - Effort: high.

Priority D — Tooling & ecosystem (parallel work, medium effort)
9) CI & tests + benchmarking harness
   - Add unit and integration tests covering session semantics, generator output, and regression tests.
   - Add small benchmark harness to measure default engine vs improved match engine.
   - Effort: low→medium.

10) Rule authoring tooling & documentation improvements
    - Create examples, an interactive playground, or a minimal web UI to list rules and run sample sessions.
    - Effort: medium.

Part D — Practical next steps (first 90 days plan)
--------------------------------------------------
Phase 1 (week 0–2): API scaffolding and tests
- Implement `ISession` interface and a basic `InMemorySession` that uses existing `DefaultRulesEngine` for `Fire()`.
- Add unit tests for session insert/update/retract and engine firing.
- Add an `Agenda` interface and initial Activation model.
Deliverable: `src/LightRules/Core/ISession.cs`, `InMemorySession.cs`, unit tests, example in `samples/`.

Phase 2 (week 2–8): Agenda & basic activation controls
- Implement agenda building during Check/Fire.
- Support activation groups and salience ordering as engine parameters.
Deliverable: agenda docs and example usage.

Phase 3 (month 2–4): Design and prototype matching engine
- Research available approaches; create a prototype for a RETE-style network or an incremental matcher.
- Add integration tests comparing results vs baseline engine.
Deliverable: prototype implementation (flagged experimental) + benchmarks.

Phase 4 (month 4+): DSL, queries, persistence, tooling
- Build DSL (LINQ-based) that maps to the pattern engine.
- Add query API, persistence hooks, and documentation.
Deliverable: first stable release with session, agenda, and optimized matcher.

Part E — Risks & tradeoffs
--------------------------
- Implementing a RETE engine is complex: long development time, tricky correctness edge-cases (joins, accumulators, negation, recursion). Consider incremental approach: deliver session + agenda first, then optimize matching for real workloads.
- API breaking changes: we already introduced a breaking immutable Facts API. Future major changes (session model or pattern engine) will also be breaking — provide compatibility shims where feasible.
- Scope creep: Drools is a very large ecosystem; focus on pragmatic parity with NRules (LINQ DSL + RETE + sessions) before considering Drools-sized features (DMN, Business Central, CEP).

Part F — Example short-term implementation task: `ISession` scaffold
------------------------------------------------------------------
- Files to add/modify:
  - `src/LightRules/Core/ISession.cs` — define Insert/Update/Retract/Fire/Check.
  - `src/LightRules/Core/InMemorySession.cs` — initial implementation using current `DefaultRulesEngine`.
  - `tests/SessionTests.cs` — unit tests for insert -> fire semantics.
- Goals:
  - Provide a stable API to migrate apps that need stateful behavior.
  - Prepare for a later replacement of the internal engine used by the session with a RETE matcher.
- Estimated effort: 2–6 days for a robust scaffold + tests.

Part G — Offer: next action I can implement for you now
------------------------------------------------------
I can implement the Phase 1 work immediately: scaffold `ISession` and `InMemorySession` plus tests and an updated sample. If you want that, I will:

- Create `ISession` and `InMemorySession` in the repo.
- Wire `samples/SampleApp` to use `ISession` (demo: Insert a fact, Fire, Update a fact, Fire again) and ensure tests pass.
- Add a short doc `docs/SESSION.md` describing the session API and migration notes.

Say "go implement ISession scaffold" and I will make the edits, run builds/tests, and report results.

---

If you want a prioritized checklist extracted as issues or a GitHub project board I can also scaffold that (create TODO files or issue templates). Which next action should I take: (A) implement `ISession` scaffold and tests now, (B) create a design doc for a RETE-based matcher, or (C) produce a migration + release-changelog file? 
