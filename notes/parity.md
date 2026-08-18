# Feature parity: Dapper vs Dapper.AOT

Status legend: ✅ supported (verified) · ⚠️ partial/constrained · ❌ not supported today ·
🚫 deliberate non-goal (decision recorded) · ❓ unverified. See [README.md](README.md) for
where the evidence comes from.

**Impact** = usefulness toward the goal (corpus density + real-world usage), zero/low/med/high.
Zero means the *concept does not exist* under AOT — e.g. pruning the ref-emit plan cache is a
zero on any scale, because there is no ref-emit plan cache in AOT. **Complexity** = initial
perceived effort to close the gap; "—" for rows already done. Both are first-pass estimates
for prioritization, expected to be revised.

"Not supported today" means the call site is left on vanilla Dapper (works under JIT, fails
under AOT) — the generator's dispatch switch marks it `NotAotSupported`, or the parameter /
result shape makes generation bail.

Two levers change several complexity scores and are worth naming up front:

- **We own Dapper too.** Where interception is blocked by Dapper's own types (e.g. an
  interceptor must *return* `SqlMapper.GridReader`, whose construction is internal), the fix
  can be an extension point added to Dapper itself — virtual members, an accessible ctor, an
  AOT-friendly interface — rather than heroics on the AOT side.
- **`[UnsafeAccessor]` (net8+)** reaches non-public members/ctors from generated C# without
  reflection, exactly as the protobuf-net AOT generator does. Several "generated C# cannot do
  what ref-emit did" limits (DAP017-adjacent) soften to "net8+ can, down-level cannot".

## 1. Core API surface (`SqlMapper` extension methods)

| Dapper API | AOT status | impact | complexity | notes |
| --- | --- | --- | --- | --- |
| `Query<T>` / `QueryAsync<T>` | ✅ | — | — | includes buffered/unbuffered flag |
| `QueryUnbufferedAsync<T>` (`IAsyncEnumerable<T>`) | ✅ | — | — | |
| `QueryFirst/Single[OrDefault]<T>` + async | ✅ | — | — | row-count guidance via DAP229/230 |
| `Query` (non-generic → `dynamic` rows) | ✅ | — | — | see §3 dynamic-row fidelity |
| `Query<object>` / untyped | ✅ | — | — | `QueryUntyped` fixture |
| `Query(Type, sql, ...)` + `First/Single[OrDefault]` + async | ❌ | med | med | needs announced types; see [type-vs-generic.md](type-vs-generic.md) |
| `Query<TFirst,...,TReturn>` multi-map (2–7 + splitOn) | ❌ | **high** | med-high | `Arity > 1` → `NotAotSupported`. New read shape (splitOn slicing, per-type readers, user delegate), all sync/async/buffered variants |
| `Query(sql, Type[] types, Func<object[],TReturn> map, ...)` | ❌ | low-med | low* | *after* multi-map + announced types land; incremental on both |
| `QueryMultiple` / `QueryMultipleAsync` (`GridReader`) | ❌ | **high** | high | interceptor must return Dapper's `GridReader` → needs a Dapper-side extension point (subclassable GridReader) or an AOT-owned grid API; then per-`Read<T>` typing is a second problem (instance calls, not interceptable — likely: announced types + runtime dispatch) |
| `Execute` / `ExecuteAsync` | ✅ | — | — | |
| `Execute` with `IEnumerable<T>` (multi-exec) | ✅ | — | low (verify) | AOT batches (`DbBatch`, `[BatchSize]`) — *better*; verify semantics match Dapper (order, transaction, partial failure, total rowcount) |
| `ExecuteScalar` / `ExecuteScalar<T>` + async | ✅ | — | — | conversion fidelity in §3 |
| `ExecuteReader` / `ExecuteReaderAsync` | ❌ | med | low-med | command setup already generated; return the (wrapped) reader; `WrappedReader`/`IWrappedDataReader` disposal semantics |
| `GetRowParser<T>(reader)` | ✅ | — | — | |
| `GetRowParser(reader, Type concreteType, ...)` | ❌ | med | low* | discriminator/polymorphism pattern; dictionary lookup once types are announced |
| `Parse<T>` / `Parse(Type)` / `Parse` (dynamic) | ❌ ❓ | low | low | same reader machinery, different entry point |
| `AsTableValuedParameter` (`DataTable` / `SqlDataRecord`) | ❓ | med | med | SQL Server crowd; pairs with `ICustomQueryParameter` |
| `AsList<T>` | n/a | — | — | trivial helper; confirm it doesn't count as a candidate site |
| `GetTypeDeserializer(Type, reader, startBound, length, ...)` | ❌ | low-med | low* | a valid raw-materializer API, not mere plumbing: with announced types it's the same dispatch map, returning a boxed `Func<DbDataReader, object>`. Its generic strengthening **already exists**: `GetRowParser<T>` (same slicing knobs), which AOT supports |
| `CreateParamInfoGenerator(Identity, ...)` | ❌ | low | med | the raw parameter-binder factory; **no generic counterpart exists in Dapper** — see "Strengthened APIs" in [type-vs-generic.md](type-vs-generic.md) for the proposed `<T>` form |
| `ReadChar` / `ReadNullableChar` / `SanitizeParameterValue` | ✅ | — | — | plain static helpers, AOT-safe as-is; nothing to intercept |
| `PurgeQueryCache` / `GetCachedSQL*` / `GetHashCollissions` / `QueryCachePurged` | 🚫 | **zero** | — | there is no ref-emit plan cache in AOT — but usage should *warn*, see §7 |
| `Format` / `ReplaceLiterals` | ❓ | low | low | falls out of the literal-injection work (see [tokens.md](tokens.md)) |
| public infrastructure statics: `PackListParameters`, `FindOrAddParameter`, `LookupDbType`, `HasTypeHandler`, `GetTypeName`/`SetTypeName`, `SetDbType`, `TypeHandlerCache<T>.Parse/SetValue`, `ThrowDataException`, `ThrowNullCustomQueryParameter` | ❓ | low | low | in scope because they are public (Contrib-style extenders call them), even though they exist to serve Dapper's generated IL. Mostly plain AOT-safe statics; the `Type`-keyed ones (`LookupDbType`, `TypeHandlerCache`) fold into announced types / the type-handler story |

## 2. Parameters (input side)

| feature | AOT status | impact | complexity | notes |
| --- | --- | --- | --- | --- |
| anonymous types / concrete POCOs | ✅ | — | — | |
| fields as members | ❓ | low | low | verify |
| `DynamicParameters` | ❌ | **high** | high | densest single gap: templates, `AddDynamicParams`, per-param DbType/direction/size/precision/scale, `Get<T>` post-execute, output callbacks. Candidate design: a real (hand-written, AOT-safe) implementation in the runtime lib that generated commands consume — generation only needed for template objects |
| `SqlMapper.IDynamicParameters` (custom impls) | ❌ | low-med | med | interface receives the `IDbCommand`, so callable directly — blocked on `Identity` (Dapper-internal) in the signature; owning Dapper permits an AOT-friendly overload |
| `SqlMapper.ICustomQueryParameter` | ❌ ❓ | med | low | interface is `AddParameter(IDbCommand, string)` — generated code can just call it |
| `IParameterLookup` / `IParameterCallbacks` | ❌ ❓ | low | low-med | obscure but public |
| `DbString` | ✅ | — | — | DAP048 nudges to `[DbValue]`; keep the Dapper spelling, the corpus uses it |
| output / return params via `[DbValue(Direction=...)]` | ⚠️ | — | — | AOT spelling works; Dapper spelling rides on `DynamicParameters` above |
| list expansion (`in @ids`) | ❌ ❓ | **high** | med | ubiquitous. Runtime rewrite helper (per-invocation list size); analyzer already detects the shape. [tokens.md](tokens.md) §2 |
| literal injection (`{=name}`) | ❌ ❓ | med | low-med | formatting rules compile-time decidable. [tokens.md](tokens.md) §3 |
| pseudo-positional (`?foo?`) | ❌ ❓ | low | med | OleDb/Access corner. [tokens.md](tokens.md) §4 |
| enum / nullable / `char` / `Guid` params | ⚠️❓ | med | low | verify edge conversions vs Dapper |
| param filtering (only bind members named in SQL) + `SupportLegacyParameterTokens` | ❓ | med | low | AOT currently *includes* + warns (DAP236); on strict providers that's an error, so may need parity not preference |
| UDTs (`UdtTypeHandler`, geo types) | ❌ ❓ | low | med | provider-specific |
| XML types (`XmlDocument`/`XDocument`/`XElement`) | ❌ ❓ | low-med | low | treat as known types with fixed handlers |
| `CommandDefinition` incl. `CommandFlags.Pipelined` | ❓ | med | low-med | `NoCache` is **zero** (no cache to bypass); `Buffered` covered; `Pipelined` is a perf feature to verify |
| `commandTimeout` / `transaction` / `commandType` args | ✅ ❓ | — | — | verify `TableDirect` |
| `CancellationToken` | ✅ | — | — | AOT extends Dapper here (DAP044/045) |

## 3. Results (output side)

| feature | AOT status | impact | complexity | notes |
| --- | --- | --- | --- | --- |
| POCO binding: props, case-insensitive | ✅ | — | — | |
| non-public members / private types | ⚠️ | med | med | ref-emit could bypass accessibility; generated C# cannot — but `[UnsafeAccessor]` (net8+) covers members; private *types* stay hard |
| constructor binding, `[ExplicitConstructor]` | ✅ | — | — | plus factory methods (AOT extension) |
| `required` / init-only members | ✅ | — | — | `RequiredProperties` fixture |
| fields | ❓ | low | low | verify |
| `dynamic` rows — behavioral fidelity | ⚠️ | high | med | the row *type* is internal and irrelevant; the observable contract is: `dynamic` member access, `IDictionary<string,object>` + `IReadOnlyDictionary<string,object>`, tolerates add/remove, specific null/missing semantics — pin that matrix with tests against AOT's row |
| tuple results | ❌ | med | med | DAP013; design already framed by `[BindTupleByName]` |
| enum results (string→enum case-insens., widening, `ShortEnum`) | ⚠️❓ | high | low-med | Dapper recently changed precedence (prefer type handlers, #2200) — match the *new* behavior |
| `MatchNamesWithUnderscores` | ❓ | med-high | low | snake_case databases; needs a compile-time equivalent (global option/attr) |
| `SetTypeMap` / `CustomPropertyTypeMap` / `ITypeMap` / `TypeMapProvider` | ❌ 🚫? | med | med | runtime config by definition; AOT spelling is `[Column]`+`[UseColumnAttribute]`. Proposal: declare 🚫 for the runtime API, ship attribute equivalents + migration guidance |
| `AddTypeHandler` / `TypeHandler<T>` / `StringTypeHandler` | ❌→⚠️ | **high** | med-high | AOT has its own `TypeHandler<T>`; needs the unification story (how does a *Dapper* handler registration become an AOT one?) |
| `AddTypeMap` / `RemoveTypeMap` (scalar DbType map) | ❌ ❓ | low-med | low | e.g. `DateTime`→`DateTime2`; global compile-time option |
| `Settings.ApplyNullValues` | ❓ | low | low | |
| coercion matrix (`char`, `Nullable<T>`, `Convert.ChangeType` fidelity) | ❓ | high | med | silent-wrongness risk; test-driven, differential against Dapper |
| column-level error reporting (`ThrowDataException` names column+value) | ❓ | med | low | DX parity worth keeping |
| `ExecuteScalar<T>` conversions (null→default, enums, handlers) | ❓ | med | low | |

## 4. Configuration & settings

| feature | AOT status | impact | complexity | notes |
| --- | --- | --- | --- | --- |
| `Settings.CommandTimeout` (global default) | ❓ | med | low | AOT has per-site args + `[CommandProperty]`; needs a global knob |
| `Settings.InListStringSplitCount` | ❌ | med | low* | *after* list expansion; SQL Server plan-stability win |
| `Settings.PadListExpansions` | ❌ | low-med | low* | same |
| `Settings.UseSingleResult/UseSingleRowOptimization` | ❓ | low | low | AOT picks `CommandBehavior` itself; verify equivalence |
| `Settings.FetchSize` (Oracle) | ⚠️ | low | low | `GlobalFetchSize` exists; verify |
| `SqlMapper.ConnectionStringComparer` | 🚫? | **zero-ish** | — | exists to partition the runtime identity/cache — a concept AOT doesn't have |
| `FeatureSupport` (per-provider null-array quirks) | ❓ | low | low | folds into list-expansion helper |

## 5. Sibling packages in the Dapper repo

Scope decision needed per package: unify, ignore, or leave on vanilla Dapper.

| package | impact | complexity | notes |
| --- | --- | --- | --- |
| `Dapper.SqlBuilder` | med | low* | runtime SQL + `DynamicParameters` — mostly falls out of the DynamicParameters work |
| `Dapper.Rainbow` | low | high | heavy runtime typing; candidate 🚫 |
| `Dapper.EntityFramework` | low | med | `DbGeography` handlers; rides on type-handler story |
| `Dapper.ProviderTools` | low | — | largely orthogonal to serialization; probably nothing to do |
| `Dapper.StrongName` etc | — | — | packaging variants, not features |

## 6. AOT-only extensions (the "plus some")

Recorded so the unified story stays a superset, not a port: batch execution (`DbBatch`,
`[BatchSize]`), cancellation via parameter members, named-tuple *parameter* binding
(`[BindTupleByName]`), factory-method construction, `[StrictTypes]`, `[QueryColumns]`,
`[CacheCommand]`, `[CommandProperty]` (provider-specific command props), `[RowCount]` /
`[RowCountHint]`, `TypeAccessor` + `SqlBulkCopy` bridge, `[IncludeLocation]`, deep TSQL
analysis (DAP2xx), `[SqlSyntax]`.

## 7. Work items arising

- **Investigate `[UnsafeAccessor]` (net8+) to lift the accessibility-class refusals.** The
  protobuf-net AOT generator uses it extensively in generated code, gated on target framework:
  it covers **construction** (`UnsafeAccessorKind.Constructor`), non-public **properties/setters**,
  **fields**, and even get-only auto-properties (writing the compiler-generated backing field) —
  and unlike reflection it is resolved at publish time, so it stays AOT-safe (protobuf-net proved
  it under ILC, including `initonly` backing fields; struct targets take `ref`). Candidates here:
  the DAP050 shapes where the *type* is accessible but its constructor is not (`DbGeography`'s
  internal ctor is exactly this), non-public setters on result members, and get-only auto-props.
  Two limits to respect, both learned in protobuf-net: the accessor's signature must still *name*
  the target type, so non-public **types** (DAP017) stay refused regardless; and it is net8.0+,
  so down-level targets keep the refusal path — protobuf-net's pattern is "smaller model with
  warnings naming the fix, not a broken build" (`DownLevelSmoke`). Probe for the attribute
  rather than assuming TFM.

- **Migrate to the modern interceptor syntax.** The generator emits the legacy
  `[InterceptsLocation(path, line, column)]` form, deprecated in current SDKs (`CS9270` — the
  generated header even carries a pragma for it, and the `SqliteUsage` snapshot warns today);
  the replacement is the `InterceptableLocation`-based version+data form. **A working
  implementation that does not need an SDK/Roslyn bump exists in protobuf-net** (just done):
  `GrpcProxyGenerator` obtains the location payload by reflecting into the host's
  `GetInterceptableLocation` (Roslyn 4.11+) so the shipped baseline can stay low, and
  `docs/aot-grpc.md` there records the encoding — reverse-engineered and proven by hand — as
  the fallback if the reflection ever stops working. Port that approach.

- **New diagnostic: warn on "has no meaning" APIs.** When AOT is enabled, detect usage of
  the APIs whose *concept* doesn't exist under AOT — the plan-cache surface
  (`PurgeQueryCache`, `GetCachedSQL`, `GetCachedSQLCount`, `GetHashCollissions`,
  `QueryCachePurged`), `CommandFlags.NoCache`, and (if confirmed zero)
  `SqlMapper.ConnectionStringComparer` — and emit a **warning**: the call is harmless but
  inert, and its presence usually signals code written to manage a runtime that is no longer
  there. Not an error: the code still runs. Next free id in the library block is DAP050
  (DAP049 is the highest taken). Distinct from DAP001 (unsupported-but-meaningful): this is
  *supported-and-meaningless*.
