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
| [parity.md](parity.md) | the feature parity table: Dapper's surface vs Dapper.AOT today |
| [tokens.md](tokens.md) | special string-token handling: `@ids` expansion, `{=literal}`, `?foo?`, etc |
| [type-vs-generic.md](type-vs-generic.md) | `Type`-based vs `<T>` APIs, and the "announce your types" design space |
| [test-suite-audit.md](test-suite-audit.md) | the Dapper test files as acceptance corpus, and what blocks each |

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
