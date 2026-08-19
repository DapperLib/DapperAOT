# Generator audit: the capture model snapshots Roslyn nodes

**Work item, and a sequencing gate: fix this before building new generator features**, so the
multi-map / DynamicParameters / QueryMultiple work is written against the fixed model rather
than needing a second pass over brand-new code.

## The rule being violated

An incremental generator's cached pipeline values must be **plain, equatable data**. Holding
`ISymbol` / `SyntaxNode` / `IOperation` / `Location` / `Compilation` in them causes two silent
failures at once:

1. **retention** — a symbol pins its entire `Compilation` (and transitively the syntax trees)
   alive for as long as the driver caches the value: in a long-running IDE session, that is
   whole Roslyn trees held non-collectible;
2. **cache defeat** — symbol/location equality does not hold across compilations, so the
   incremental cache never hits anyway; you pay the leak *and* get none of the benefit.

## Findings (2026-08-18, `main` @ `e3b1037`)

Verified by reading the code, not assumed:

- **`DapperInterceptorGenerator.SuccessSourceState`** (`DapperInterceptorGenerator.cs:1505`)
  holds `IMethodSymbol Method`, `ITypeSymbol? ResultType`, `ITypeSymbol? ParameterType`, and
  `Location` — this is the cached per-call-site value (`CreateSyntaxProvider(PreFilter, Parse)`).
  `CommonComparer` groups with `SymbolEqualityComparer`, which also does not hold across
  compilations.
- **`TypeAccessorInterceptorGenerator.SourceState`** (`TypeAccessorInterceptorGenerator.cs:184`)
  same shape: `Location` + `ITypeSymbol` + `IMethodSymbol`.
- **`MemberMap`** (`Internal/MemberMap.cs`) holds `ITypeSymbol`s, `IMethodSymbol`s, a
  `Location`, and an **`IOperation`** — and is reachable from `AdditionalCommandState`, which
  rides inside `SuccessSourceState`. Audit everything transitively reachable from the cached
  states; a single symbol anywhere in the graph is enough to pin the compilation.
- **The pipeline combines the raw compilation**: `DapperInterceptorGenerator.cs:58-61` —
  `context.CompilationProvider.Combine(nodes.Collect())` feeding `RegisterSourceOutput`. Even
  with a clean node model, this re-runs the generate step on *every* compilation change
  (every keystroke); the compilation must not be an input to the output step. (This is the
  documented Roslyn anti-pattern; RS1041-family guidance.)
- `FaultSourceState` holds `Exception` + `Location` — same treatment (message/type name +
  span data).

Net: the generator currently behaves as a **full-recompute generator with a memory leak**,
not an incremental one. Correctness is unaffected — which is why nothing ever flagged it.

## The fix shape (proven in protobuf-net's AOT generator)

This exact trap sank earlier attempts at protobuf-net's generator; the working pattern there:

- model types are hand-written **equatable plain-data values** — strings, enums, packed
  flags, hand-rolled `EquatableArray<T>` (note `ImmutableArray<T>` equality is
  reference-based and silently defeats caching too — don't swap one trap for another);
- locations are stored as Roslyn **value** types (`TextSpan` + `LinePositionSpan` + path —
  plain data), reconstituted into a `Location` only at report/emit time (`PlanLocation`);
- symbols are fully **projected during parse**: everything emit needs (qualified type names,
  member lists, flags) is extracted into the model while the `SemanticModel` is in hand;
- diagnostics ride a **separate** pipeline branch from the emit model, because they carry
  locations that shift with every edit and the emit model should not;
- a **shape-enforcing test** walks the model types by reflection and fails on any field of a
  Roslyn reference type (`ProtoModelPlanShapeTests` pattern) — the constraint has to have
  teeth or it erodes; it eroding silently is precisely how it got here.

The interceptor output itself needs `InterceptsLocation` data — that is file/position values,
not `Location` objects, so it survives projection fine.

## Why fix-first is the right order

Every gap-closing feature in [parity.md](parity.md) §1–§3 adds parse-time state (multi-map
adds per-type splits, DynamicParameters adds member graphs, announced types add a type
inventory). Built on the current model, each addition deepens the symbol snapshot and has to
be re-done when the model is fixed. Built after the fix, each lands on plain data from day
one. The harness ([harness-baseline.md](harness-baseline.md)) is unaffected — it measures
behavior, not model shape — so the two tracks can run in parallel.
