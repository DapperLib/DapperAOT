# Dapper / Dapper.AOT unification — working notes

These notes are the working set for closing the gap between vanilla Dapper and Dapper.AOT,
with the eventual goal of retiring ref-emit entirely.

## The success criterion

> We turn Dapper.AOT "on" in the **Dapper test suite** (`Dapper/tests/Dapper.Tests`), announce
> the types in play via (new) attributes, and Dapper.AOT swallows *everything* — every call site
> intercepted, all tests green, with Dapper.AOT doing all the work. Bonus: the project compiles
> in AOT mode without warnings.

That makes the Dapper test suite the **acceptance corpus**: parity is not "the API list looks
covered", it is "these tests pass through generated code". The corpus conveniently already
exercises the obscure corners (list expansion, literals, pseudo-positional parameters, type
handlers, multi-map, GridReader, dynamic rows, output parameters, ...), across many providers.

"Announce the types" is the one concession we ask of consumers: the `Type`-based (non-generic)
APIs and anything else that discovers types at runtime need the candidate types stated at
build time. See [type-vs-generic.md](type-vs-generic.md).

## The documents

| doc | contents |
| --- | --- |
| [plan.md](plan.md) | **the agreed plan**: complete the gap table → fix the generator → close the gaps |
| [parity.md](parity.md) | the feature parity table: Dapper's surface vs Dapper.AOT today |
| [tokens.md](tokens.md) | special string-token handling: `@ids` expansion, `{=literal}`, `?foo?`, etc |
| [type-vs-generic.md](type-vs-generic.md) | `Type`-based vs `<T>` APIs, and the "announce your types" design space |
| [test-suite-audit.md](test-suite-audit.md) | the Dapper test files as acceptance corpus, and what blocks each |
| [harness-baseline.md](harness-baseline.md) | real numbers from the suite with AOT enabled (Dapper repo, `aot-harness` branch) |
| [dynamicparameters-design.md](dynamicparameters-design.md) | phase 3 item 1: delegate to the bag's own vanilla protocol; needs a small Dapper-side API |
| [generator-audit.md](generator-audit.md) | **fix-first gate**: the capture model snapshots Roslyn symbols/nodes — retention + cache defeat |

## Scope: the public API, by observable behavior

Parity is defined over Dapper's **public API** (`Dapper/PublicAPI.Shipped.txt` is the
checklist) and the **observable behavior** of each member: the SQL text and parameter set
that reach the provider, and the values that come back. How Dapper implements any of it —
internal types, regexes, caches — is irrelevant, except as *evidence* of the observable
behavior. Two consequences:

- internal implementation types (e.g. the dynamic-row class) need behavioral fidelity (the
  returned object's public contracts), never type fidelity;
- public-but-infrastructure members (`PackListParameters`, `FindOrAddParameter`,
  `TypeHandlerCache<T>`, ...) are in scope *because they are public* — Contrib-style
  extenders call them — even though they exist to serve Dapper's own generated IL.

## Honesty rules

- A status in these tables is only worth having if it was **verified against code** — the
  interceptor's dispatch switch, a test fixture, or the Dapper source. Anything inferred or
  remembered is marked ❓ until checked.
- "Compiles" is not "works": a call site left un-intercepted still compiles and passes tests
  on a JIT runtime via vanilla Dapper. The corpus only measures us when interception is
  *confirmed* (DAP000 counts, or Dapper is removed from the runtime closure).
- Statuses: ✅ supported (verified) · ⚠️ partial/constrained · ❌ not supported today ·
  🚫 deliberate non-goal (decision recorded) · ❓ unverified.

## Where the current status came from

- Dapper.AOT's supported-method set: the dispatch switch in
  `src/Dapper.AOT.Analyzers/Internal/Inspection.cs` (`IsDapperMethod`, `OperationFlags`).
- Fixture evidence: `test/Dapper.AOT.Test/Interceptors/*.input.cs` — a fixture with an
  `.output.cs` generates; one without (e.g. `QueryMultiple`, `QueryMultiType`,
  `DynamicParameters`) does not.
- Dapper's surface: `Dapper/Dapper/PublicAPI.Shipped.txt` plus source (`SqlMapper.cs` for the
  token machinery).
