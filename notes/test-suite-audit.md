# The Dapper test suite as acceptance corpus

Goal restated: enable Dapper.AOT in `Dapper/tests/Dapper.Tests`, announce types where the
`Type`-based APIs need it, and have every call site intercepted with all tests green —
ideally AOT-publishable without warnings.

## Practicalities

- The suite needs live databases. `tests/docker-compose.yml` exists in the Dapper repo;
  installing SQL Server Developer Edition locally or using docker images is fine on this
  machine. SQL Server is the core dependency (`TestBase` runs everything against both
  `System.Data.SqlClient` and `Microsoft.Data.SqlClient`); the provider tests add SQLite,
  MySQL, Postgres, Firebird, DuckDB, Snowflake, OleDb, Linq2Sql, EF.
- Interception requires C# ≥11 + net8 SDK and `<InterceptorsPreviewNamespaces>`; the test
  project must opt in.
- **Measurement**: DAP000 reports "handled N of M possible call-sites". The corpus number to
  drive to 100% is that ratio (per test assembly), then the test pass rate, then the AOT
  publish warning count — in that order. A passing test with an un-intercepted call site is
  measuring vanilla Dapper, not us.
- **The DAP list is useful but incomplete: some failure modes are silent until executed.**
  "Handled" at build time means an interceptor was emitted, not that it behaves like Dapper —
  a generated call can bind the wrong member, format a value differently, or fail only on a
  particular data shape, with no diagnostic anywhere. So the build-time ratio is an *upper
  bound*; only the test run catches silent divergence, which is why the DB-backed run is part
  of the measurement and not an optional extra. (This is the same lesson as protobuf-net's
  AOT differential: every serious generator bug there compiled cleanly and wrote wrong bytes.)
- **Decided:** tests that assert *Dapper internals* (cache counts, `Identity`, deserializer
  internals) are expected to be **adjusted, not maintained** — those side-effect numbers are
  not part of the contract, and several describe machinery AOT doesn't have. Per test: skip
  under AOT, or replace with an assertion about the observable behavior it was standing in
  for. "100% of the corpus" means 100% of the *behavioral* corpus.

## Per-file first-pass audit

Status here = "expected blockers when AOT is enabled", from file names + known contents;
refine by actually running with interception enabled and reading DAP000/DAP001 output.

| test file | exercises | expected blockers today |
| --- | --- | --- |
| `QueryMultipleTests` | `QueryMultiple`/`GridReader` incl. multi-map reads, unbuffered grids | ❌ QueryMultiple wholesale |
| `MultiMapTests` | `Query<T1..T7,TReturn>`, splitOn variants, `Type[]` overload | ❌ multi-map wholesale |
| `ParameterTests` | DynamicParameters (templates, output, callbacks), list expansion incl. padding + string_split, TVPs, `ICustomQueryParameter`, `DbString`, UDTs, pseudo-positional | ❌ most of it: the single densest gap file |
| `LiteralTests` | `{=name}` injection, enums/bools as literals, in-list + literal combos | ❌ literal injection |
| `TypeHandlerTests` | `AddTypeHandler`, `StringTypeHandler`, `ITypeHandler`, `RemoveTypeMap`/`AddTypeMap` | ❌ runtime registration model |
| `PreferTypeHandlersForEnumsTests` | new enum/type-handler precedence (#2200) | ❌ same |
| `EnumTests` | enum coercions: string→enum, nullable, `ShortEnum`, in-list of enums | ⚠️ verify coercion matrix |
| `ConstructorTests` | ctor binding, `[ExplicitConstructor]`, mixed ctor+setter | ✅ mostly; verify edge rules match |
| `TupleTests` | tuple results and parameters | ❌ tuple results (DAP013/014) |
| `AsyncTests` | full async surface incl. unbuffered, cancellation, `Pipelined` flag | ⚠️ mostly ✅; `CommandFlags.Pipelined`/`CommandDefinition` paths ❓ |
| `SingleRowTests` | First/Single[OrDefault] semantics incl. empty/over-read | ✅ verify exception parity |
| `NullTests` | null handling, `ApplyNullValues` | ❓ |
| `DecimalTests`, `DateTimeOnlyTests` | numeric coercion; `DateOnly`/`TimeOnly` | ⚠️ AOT has DateOnly fixture; verify |
| `XmlTests` | XmlDocument/XDocument/XElement params+results | ❌ inbuilt XML handlers |
| `DataReaderTests` | `ExecuteReader`, `GetRowParser` (incl. `Type`-based discriminator), `Parse` | ❌ ExecuteReader, Type-based parser |
| `WrappedReaderTests` | `IWrappedDataReader` unwrap behavior | ❌ rides on ExecuteReader |
| `TransactionTests` | transaction plumbing incl. `TransactedConnection` (custom `IDbConnection` wrapper!) | ⚠️ custom `IDbConnection` impls — interceptor targets extension calls on the interface, so should be fine, but the *generated* code paths for non-`DbConnection` need checking |
| `ProcedureTests` | stored procs, output/return params via DynamicParameters | ❌ DynamicParameters.Get pattern |
| `MiscTests` | the grab-bag: dynamic rows semantics, `AsList`, huge param counts, field binding, private members, generic helper methods calling Dapper (`TestSubsequentQueriesSuccess<T>` style) | ⚠️ several: **generic helper methods** are a known structural gap (DAP016: generic type parameters not supported) — indirect/helper usage is explicitly "not today" in the FAQ |
| `TypeAttributeTests`* / column mapping (in Misc/others) | `SetTypeMap`, `CustomPropertyTypeMap`, `MatchNamesWithUnderscores` | ❌ runtime type-map APIs |
| `SqlBuilderTests` | Dapper.SqlBuilder templates | ❌ rides on DynamicParameters |
| `Providers/*` | SQLite/MySQL/Postgres/Firebird/DuckDB/Snowflake/OleDb/EF/Linq2Sql | ⚠️ per-provider: `$`/`:`/`?` prefixes, pseudo-positional (OleDb), `DbGeography` (EF), provider-specific types |
| `ProviderTests` | `IDbConnection` vs `DbConnection` API split, `GetRowParser` polymorphism | ⚠️/❌ |

\* exact file/test names to be confirmed when the suite is run with interception on — this
table was drawn from file names and Dapper source knowledge, not yet from a run.

## Suggested sequencing (by corpus unblocked per unit of work)

1. **DynamicParameters** — unblocks `ParameterTests`, `ProcedureTests`, `SqlBuilderTests`;
   biggest single win and forces the output-parameter design.
2. **List expansion + literals + pseudo-positional** (one work item: the SQL-rewrite runtime
   helper) — unblocks `LiteralTests`, much of `ParameterTests`, provider corpora.
3. **QueryMultiple/GridReader** — needs an AOT-owned GridReader equivalent; unblocks
   `QueryMultipleTests` and parts of others.
4. **Multi-map** — `MultiMapTests`; generic arities first, `Type[]` after announcements land.
5. **Announced types** ([type-vs-generic.md](type-vs-generic.md)) — `Type`-based APIs,
   `GetRowParser` discriminator, `Parse`.
6. **Type handlers unification** — decide the runtime-registration story; unblocks
   `TypeHandlerTests`, `XmlTests`, EF geo types.
7. **ExecuteReader/WrappedReader**, tuple results, `ApplyNullValues`, settings parity — the
   tail.

Items 1–2 are also exactly where "obscure API usage" concentrates, which is why the corpus
ordering and the feature ordering agree.

## Indirect usage (helper methods)

The corpus (like real code) wraps Dapper in generic helpers (`Get<T>`, repository patterns).
Interceptors work per-call-site with concrete types, so `helper<T>` bodies calling
`connection.Query<T>` are today's DAP016. The "announce your types" mechanism may double as
the answer here (instantiate the helper's generated code per announced type) — worth keeping
the two designs joined up.
