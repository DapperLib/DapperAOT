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
and works with every shipped Dapper. (Superseded in part by the amendment at the end: tier 1
still lands first, but behind an opt-in, so "needs no consumer changes" now means "needs one
module-level attribute".)

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

## Direction (agreed 2026-08-21): declarative config attributes as the primary spelling

A static `Dapper.SomeConfigCall(...)` becomes `[module: SomeDapperConfig(...)]` - and that
is *better scoped*, not merely equivalent: per-assembly instead of process-global mutable
state, deterministic (no startup-ordering races), reviewable in the diff, and
compile-time-visible so the generator bakes it at zero runtime cost. The protobuf-net
precedent carries over whole, including the cross-assembly hand-off: the generator gathers
assembly-level declarations from *references*, so a package can ship handlers for the
types it owns (the [ProtoSurrogate] pattern, probed and shipped there).

The config-call surface mapped onto attributes:

| runtime call | declarative spelling |
| --- | --- |
| `SqlMapper.AddTypeHandler(typeof(T), h)` | `[module: TypeHandler<T, THandler>]` - already ships (dormant); generator wires it, announced handlers *elide* the runtime dispatch for that type |
| `SqlMapper.AddTypeMap(type, dbType)` | `[module: TypeMap(typeof(string), DbType.AnsiString)]` (new) - also answers the deferred AnsiString pair with zero per-parameter cost |
| `Settings.*` globals (CommandTimeout, list-expansion knobs...) | `[module: DapperSettings(...)]`-style (new); several parity rows already wanted "a compile-time global" |
| `DefaultTypeMap.MatchNamesWithUnderscores` | same treatment (parity row already asks for it) |
| `SetTypeMap` / `CustomPropertyTypeMap` | stays 🚫; `[Column]` + `[UseColumnAttribute]` is the spelling |

Layering (unchanged from the PR, sharpened by the discussion):

1. tier 1 (PR #206) stays, but **off by default** - see the amendment below;
2. tier 2 attributes become the *recommended* spelling, statically dispatched; the
   migration story is tooling, not docs alone - an analyzer that spots
   `SqlMapper.AddTypeHandler(...)` in a [DapperAot] compilation and offers the attribute
   as a code fix (the AotMigrationAnalyzer pattern from protobuf-net);
3. a strict switch turns the runtime bridge off entirely (closed world, trimmable) for
   consumers who want the full protobuf-net posture.

## Amendment (2026-08-23): the default is closed, and the switch loosens it

Item 3 above had it backwards: making strictness opt-in leaves every consumer in the open-world
posture by default, which is the one AOT cannot support. The switch is now
`[module: UseRuntimeTypeHandlers]`, **default off**, and it *loosens* rather than tightens.

Why the runtime path cannot be the default, beyond the per-operation lookup:

- **It is not AOT-safe.** What it defers to reaches `SqlMapper.TypeHandlerCache<T>` - a generic
  instantiated over a runtime-chosen type. ILC cannot know which instantiations to keep and
  nothing warns at publish; issue #165 is that crash on a deployed app. DAP053 now reports the
  `[UseRuntimeTypeHandlers]` + `PublishAot` combination at build.
- **It keeps the world open**, so nothing reachable from the registry can be trimmed.
- **It is unverifiable at build**, where a declared handler can be checked (unusable handler,
  duplicate registration, unregistered type).
- **Ordering.** Generated code baked its decision at compile time; a registration arriving later
  is either ignored or forces a per-operation check to catch it. There is no third option.

Scope: assembly/module only. A handler registration is a property of a *type*, so it cuts across
every call-site touching that type; per-method scope would let one type bind two ways in one
process. `AttributeUsage` enforces this, so a misplaced application is a compiler error rather
than a diagnostic we have to invent.

Cost of the gate when off: the emitted dispatch disappears from every call-site (which is why
the ~100 golden files this PR used to touch are untouched again), and the module initializer that
installs the read bridge is not emitted, so `TypeHandlerBridge.Has`/`TryParse` short-circuit on a
null delegate. The residue is one null check on the flexible read path and one `Resolve` call per
query.

What this buys the corpus: the Dapper test suite keeps its runtime registrations and stays at
705/793 with a single `[module: UseRuntimeTypeHandlers]` in the harness - no test edits - while
everyone else gets the closed-world default. That retires the earlier
"restructure the suite vs. bridge-on-by-default" choice entirely.
