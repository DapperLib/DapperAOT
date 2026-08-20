# Type handlers: the unification story

## What the failing tests actually are

"TypeHandlerTests ×16/provider" decomposes into four families; only the first two are
type-handler work:

1. **Runtime `AddTypeHandler` registrations** (Issue136, Issue1959 ×2, Issue461,
   Issue253 ×2, SO24740733 ×2, EnumTypeHandler-preference): the suite registers handlers
   at runtime; write-side members bind raw ("No mapping exists from object type
   LocalDate…" from the provider) and read-side members never consult the handler.
   Issue253 is the sharp one: a *handled collection type* — vanilla checks handlers
   **before** list expansion, and our #197 expansion now wins incorrectly.
2. **`AddTypeMap`** (AnsiString ×2): runtime remap of a *recognized* scalar's DbType.
3. **`SetTypeMap`/`CustomPropertyTypeMap`** (TestCustomTypeMap, Test_RemoveTypeMap):
   runtime *column-name mapping* — genuinely incompatible with compile-time row
   factories; the parity table's 🚫-proposal stands (decision needed).
4. **Coercion tail wearing the wrong filename** (TestBigIntForEverythingWorks: enum
   from float/double column needs the pre-convert vanilla does; Issue149 strictness):
   not handler work at all.

## Tier 1: runtime dispatch, delegating to vanilla's own decision procedure

`SqlMapper.LookupDbType(Type, name, demand, out ITypeHandler)` is public and
`[Obsolete(…, false)]` — the same suppressible tier as `PackListParameters`, and it *is*
the whole vanilla decision: handlers, the `AddTypeMap` remap, LinqBinary,
`Settings.PreferTypeHandlersForEnums`, and `EnumerableMultiParameter` (i.e. the
handler-before-expansion ordering), evaluated at execution time. `ITypeHandler` itself
(`SetValue(IDbDataParameter, object)` / `Parse(Type, object)`) is public and
non-obsolete. (`TypeHandlerCache<T>` is obsolete-as-**error** — unusable from generated
C#, which is why vanilla can only call it from IL; no new Dapper API is needed, so no
DAP052 gate.)

- **Write, unknown member type** (today: raw `p.Value = …`, provider throws): emit
  `LookupDbType(typeof(X), name, demand: true, out var handler)`; handler present →
  `handler.SetValue(p, value)` with the **raw** value (null stays null — Issue1959 pins
  that the handler sees null; vanilla only sanitizes on the non-handler path); else
  apply the returned DbType if any and bind as today. `demand: true` also restores
  vanilla's *"The member X of type Y cannot be used as a parameter value"* — which is
  exactly what MiscTests.TestUnexpectedDataMessage pins, so that clears too.
- **Write, expandable member**: same lookup *first*; handler present → single handled
  parameter, else `PackListParameters` (vanilla's ordering; fixes Issue253).
- **Write, enum member**: branch on `Settings.PreferTypeHandlersForEnums` (static bool,
  default false — cheap short-circuit) before the baked enum path.
- **Read, unknown member/result type**: `LookupDbType(typeof(X), "", demand: false,
  out var handler)`; handler present → `(X)handler.Parse(typeof(X), reader.GetValue(i))`,
  else the current `As<X>` fallback. Per-row lookup for tier 1 — registrations are
  mutable (the suite re-registers), so per-shape caching is a later optimization with a
  staleness story, not a first cut. Covers constructor binding (Issue461) and the
  single-column scalar form (SO24740733).

Deliberately *not* in tier 1:

- **`AddTypeMap` on recognized scalars** (the AnsiString pair): honoring it means every
  string member pays a runtime lookup where today the DbType is baked. Possible, small,
  but a per-parameter cost on the most common parameter type — decision to take
  explicitly rather than slip in.
- **`SetTypeMap` family**: propose 🚫 (runtime column-mapping vs compile-time row
  factories); the attribute equivalents (`[Column]` + `[UseColumnAttribute]`) are the
  AOT spelling.

## Tier 2: the announced-attribute layer (compile-time)

`[TypeHandler<TValue, THandler>]` and `TypeHandler<T>` already ship in Dapper.AOT — the
generator just never consults them (dormant API). Wiring them gives static dispatch
(no lookup, no mutable registry, trim-friendly) and is the AOT-strict spelling to point
people at. Prior art: external PRs #117 (samcragg — the attribute shape, plus a
`Read(DbDataReader, int)` addition to `TypeHandler<T>`) and #162 (7amou3 — static
per-file handler instances instead of per-call `new`). Both are the right *shape*;
neither implementation can land as-is post-phase-2: #162's `TypeHandlerInstanceRegistry`
keys a dictionary on `INamedTypeSymbol` inside generator state, which is exactly the
Roslyn-objects-in-cached-state trap the plain-data model exists to prevent (ModelShapeTests
enforces it). Tier 2 = their design, re-done as plain-data plans, with credit.

Tier 1 first: it is what the test suite actually measures, needs no consumer changes,
and works with every shipped Dapper.

## Outcomes (recorded after implementation)

- **695 -> 705/793**: the whole runtime-handler family cleared (Issue136, Issue1959 x4,
  Issue253 x2, Issue461, SO24740733 x2, Issue149, the enum-preference test), plus the bare
  `DataTable` TVP pair and the Xml tests - vanilla registers `DataTableHandler` and the XML
  handlers *by default*, so the dispatch reaches them for free.
- **`demand: false`, not vanilla's `demand: true`**, deliberately: when nothing matches we
  keep the previous raw bind, because modern providers natively handle types vanilla's map
  does not (DateOnly until the Dapper re-enable ships being the live case). Message parity
  for genuinely-unusable types (TestUnexpectedDataMessage) is deferred to that bump.
- **A handler receives DBNull, never null** - `SqlMapper.TypeHandler<T>`'s explicit
  interface impl special-cases DBNull and NREs on a raw null (struct cast); vanilla's IL
  coalesces first, so we do too.
- **`char`/`char?` stay excluded from dispatch**: their map entry is StringFixedLength
  *with* SetType, and applying it pads the round-trip (TestCharInputAndOutput). Vanilla
  converts char members to length-1 strings on the way out - coercion-tail work, not
  handler work.
- **The build-exit lesson, again**: the first measurement showed zero movement because the
  harness build had silently failed (generated `typeof` on an annotated reference type is
  CS8639, on `dynamic` CS1962 - hence `ParamMember.TypeOfName`, mirroring `RowMember`'s)
  and `--no-build` ran stale binaries. Check the exit code, not the presence of output.
