# Harness baseline: Dapper.AOT enabled in the Dapper test suite

First real numbers, 2026-08-18. Setup lives on the `aot-harness` branch of the **Dapper**
repo (sibling checkout; deliberately **local-only, not pushed** — it is a measurement rig,
not work-in-progress on the public repo): local package feed at `../DapperAOT/artifacts` (pack with
`NBGV_GitEngine=Disabled dotnet pack src/Dapper.AOT/Dapper.AOT.csproj -c Release -o artifacts`,
giving the stable harness version `1.0.0-g`; purge `~/.nuget/packages/dapper.aot/1.0.0-g`
between repacks), `[module: DapperAot]` in `DapperAotEnable.cs`, interceptors enabled, and a
`.globalconfig` raising DAP000 (Hidden by default) to warning.

Build: `dotnet build tests/Dapper.Tests/Dapper.Tests.csproj -f net10.0` (net481 and net8.0
legs not yet measured).

## The headline, and why it is wrong

> `DAP000: Dapper.AOT handled 396 of 396 possible call-sites using 143 interceptors, 61
> commands and 15 readers`

…reported alongside **~96 compile errors, all in the generated file**. Two separate honesty
problems, both now work items:

1. **The denominator excludes what was never attempted.** The suite plainly uses
   `QueryMultiple`, multi-map, `ExecuteReader`, … (whole test files of them) — "396 of 396"
   counts only call-sites the generator considers candidates, so the unsupported surface
   vanishes from the score. The scorecard should count *every* Dapper call-site, splitting
   into handled / unsupported (with ids) / failed.
2. **"Handled" does not even mean "compiles"**, let alone "behaves like Dapper" — the
   emitted code failed to build. (And behavioral divergence is silent until executed — see
   the measurement caveat in [test-suite-audit.md](test-suite-audit.md).)

## Error classes → root causes

Raw tally (log lines; MSBuild double-lists, so ratios matter more than counts): CS0122 ×76,
CS1527 ×70, CS0708 ×24, CS0246 ×12, CS0102 ×6, CS0115 ×2, CS0052 ×2.

Actual distinct bugs: **two**.

- **Bug A — array-of-anonymous-type parameter wrecks the file.** `MiscTests.cs:1217`-ish:
  a parameter object with a member `MyModels = new[] { new { XXXId = 1, ... }, ... }`. The
  unary anonymous-type path correctly emits `CommandFactory<object?>` + a
  `Cast(args, static () => new { ... })` shape-witness (visible in the generated
  `CommandFactory26`); the enumerable path instead renders the *display string* into source:
  `CommandFactory<global::<anonymous type: int XXXId, ...>[]>` — which is not C#. The `<`/`>`
  in that string break the parse tree, so every subsequent class in the file lands at
  namespace scope: the CS1527/CS0708/CS0102/CS0115/CS0246/CS0052 flood (and the CS0122s
  naming `CommandFactoryNN`) are **all cascade from this one bug**. It also emits
  `args.XXXId` where `args` is the array — the body is wrong as well as unnameable.
- **Bug B — inaccessible types are referenced instead of refused.** `PostgresqlTests.Cat` is
  a `private` nested class used as a row type; the generator emitted
  `RowFactory<...PostgresqlTests.Cat>` anyway → genuine CS0122. DAP017 ("non-accessible
  type … not currently supported") exists for exactly this, but the *generator* doesn't
  enforce it — it should drop the call-site (leave it on vanilla Dapper) with the diagnostic,
  never emit code that cannot compile. Same principle as protobuf-net's PBN3002: refuse
  beats emitting a build break in a file the consumer never wrote.

## Anomaly: zero analyzer diagnostics

The build shows **no** DAP diagnostics at all besides DAP000 — no DAP001 (unsupported
method: the suite is full of them), no DAP2xx SQL analysis, and no AD0001 (so the analyzer
didn't crash). Hypothesis: the broken generated code poisons binding, and the analyzer's
operation callbacks bail on error symbols. Re-check once Bug A is fixed; if the silence
persists with a clean compile, that's its own bug.

## Round 3 (same day): four fixes in, the suite compiles

With PRs #180 (generic args in accessibility), #181 (enumerable params outside Execute),
#182 (member types checked like the parameter type), #183 (GetRowParser over IDataReader),
plus a harness-side severity downgrade of DAP036/DAP037 (they fire as *refusals* on the
scalar-result feature gaps — enums, char, TimeSpan, arrays, dynamic — so for measurement
they are gap markers, not build breaks):

> **exit 0.** `Dapper.AOT handled 387 of 387 possible call-sites using 137 interceptors,
> 55 commands and 15 readers` — and zero compiler errors.

The 550 DAP warnings are now the measured gap surface (DAP027 ×402, DAP028 ×44, DAP012 ×40,
DAP018 ×26, DAP048 ×16, DAP036/037 ×20 as downgraded errors). Next instruments: the honest
scorecard (the 387 denominator still excludes never-attempted APIs), then actually *running*
the suite against a database, which is where silent divergence shows.

## Round 4: the honest scorecard (PR #185)

With DAP000 counting every enabled call-site (unsupported APIs and diagnostic-refused sites
included), the truth behind the former "100%":

| leg | handled | of | ratio | unsupported API | skipped via diagnostics |
| --- | --- | --- | --- | --- | --- |
| net10.0 | 387 | 725 | **53.4%** | 82 | 256 |
| net8.0 | 387 | 725 | 53.4% | 82 | 256 |
| net481 | 405 | 757 | 53.5% | 84 | 268 |

So the corpus number to drive to 100% starts at **53%**. Harvested breakdown (net10.0,
diagnostic occurrences; the refusal ids default to *Info* and needed elevating in the harness
globalconfig to be visible at all — worth knowing before trusting any warning tally):

| bucket | count | meaning |
| --- | --- | --- |
| DAP016 | 272 | **types nested inside `XxxTests<TProvider>`** — the DTO itself is generic-free; only its containment involves the type parameter. A corpus-shape artifact, not real-world weight: see the decision below |
| DAP015 | 100 | untyped parameters (`DynamicParameters` / `object`) — confirms DynamicParameters as phase-3 #1 |
| DAP001: `QueryMultiple(Async)` | 54 | unsupported API |
| DAP001: multi-map `Query<...>` | 52 | unsupported API |
| DAP001: `ExecuteReader(Async)` | 42 | unsupported API |
| DAP013/DAP014 | 40 | tuple results/params |
| DAP017 | 22 | non-public types (private DTOs in the test classes) |
| DAP037/DAP036 | 20 | construction gaps (scalar-ish results, type-handler territory) |

**Decision needed on the DAP016 bucket** (the single largest): the honest options are (a)
adjust the *suite* — move the nested DTOs to namespace scope, which changes zero observable
behavior and is in the same spirit as "announce your types"; (b) generator support for
call-sites whose types are open over the *containing* class's type parameters — hard, and
possibly not expressible with interceptors at all (the interceptor method cannot see the
enclosing class's type parameters); or (c) accept the ceiling. (a) looks right, but it is a
change to Dapper's test layout, so it is an explicit call, not something to slip in.
Similarly DAP017's 22 sites are private test DTOs, where `internal` would do.

## Round 5: the first behavioral run (local SQL Server 2022)

Control (vanilla, `main` worktree): **760 passed, 0 failed, 29 skipped** (unavailable
providers), 22s. Same suite with AOT interception on: **676 passed, 84 failed** — 42 per SQL
Server provider, perfectly symmetric.

**Every one of the 84 compiled clean.** This is the measurement caveat made flesh: the
call-sites were "handled", and the failure only exists at runtime. Breakdown by class:

- **list expansion** (`in @ids`, empty arrays, string_split, padding): the largest group —
  `ArgumentException: No mapping exists from object type System.Int32[]...` — the generated
  code binds the array member as a raw parameter value; ParameterTests/AsyncTests/MiscTests;
- **literals** (`{=name}`): LiteralTests + LiteralInAsync etc;
- **TVPs / `SqlDataRecord` / `ICustomQueryParameter`**: bound raw, same ArgumentException shape;
- **type handlers** (AnsiString default, `RemoveTypeMap`, IEnumerable handler): 4/provider;
- **a small coercion tail worth individual triage**: OverflowException ×2, InvalidCastException
  ×2, RuntimeBinderException ×2 (dynamic single-row), one transaction-inheritance case, one
  DataReader case — these may be genuine behavioral divergences at handled sites rather than
  known feature gaps, i.e. exactly the class nothing but this run can find.

The encouraging half: 676 tests pass *with 387 interceptions live*, so interception itself is
broadly sound — failures concentrate precisely where the parity table said the gaps are, which
also re-validates the phase-3 order (tokens and type handlers carry real runtime weight, not
just call-site counts).

## Round 6: DAP051 + the corpus restructure (decision: pre-a then a)

Marc's call: first make the nested-DTO problem obvious to users - **DAP051** (PR #192) splits
the "generic only by containment" shape out of DAP016, names the culprit container in the
message, links a docs page with the concrete before/after (move `Dog` out), and is a
*warning* (Info is invisible in MSBuild output). Then restructure the suite: every such DTO
(84 types, 12 files) moved into per-file non-generic `XxxTestsTypes` containers with
using-static imports - zero behavioral change, unqualified usages intact. NullTests and
ProcedureTests stay nested deliberately as pinned DAP051-ceiling representatives.

Result: **494 of 725 handled (68.1%)**, up from 53.4%; DAP051 collapses 272 → 4 (the pins).
The sweep immediately exposed a new generator bug on a first-time shape - a `dynamic`-typed
member emitted illegal `typeof(dynamic)` (CS1962) - fixed as PR #193 with a golden fixture.
Remaining skip buckets: DAP015 ×114 (untyped params - DynamicParameters is now unambiguously
phase-3 #1), DAP013/14 ×40 (tuples), DAP016 ×28 (genuinely-generic incl. helper methods),
DAP017 ×22 (private types), DAP037 ×20 + DAP050 ×12 (construction), DAP036 ×4.

## Round 6b: behavioral run on the restructured suite

**612 passed / 148 failed** (74 per provider, symmetric; vanilla control remains 760/0).
Failures *rose* from 84 as interception rose from 387 to 494 - which is the honest direction:
the newly-intercepted call-sites exercise runtime gaps that were previously hidden behind
DAP051 refusals. The growth is concentrated exactly where expected: TypeHandlerTests 4 → 16
per provider (the DTOs now generate, and `SqlMapper.AddTypeHandler` registrations are
ignored by generated code - the phase-3 type-handler story), MiscTests 10 → 16, plus the
existing token/TVP groups. Breakdown: ArgumentException ×56 (raw-bound lists/TVPs),
assertion failures ×30 (behavioral divergence at handled sites - the coercion-matrix tail,
worth triage), InvalidCast ×8, SqlException ×16, NotSupported ×8.

**[NOTE]** run duration jumped 22s → 9m26s - some newly-intercepted failing tests appear to
burn full command timeouts; worth a look during phase 3 (it makes the behavioral loop slow).

## Round 7: DynamicParameters lands (delegate-to-the-bag)

Design in [dynamicparameters-design.md](dynamicparameters-design.md); the Dapper-side
`AddParameters(IDbCommand)` overload sits on the local `dynamicparameters-apply` branch
(API shape awaiting Marc's sign-off), and the generator support (probe-gated: older Dapper
keeps the DAP015 refusal, goldens untouched) is on `dynamicparameters-support`.

**533 of 725 handled (73.5%)**, up from 68.1%; DAP015 falls 114 → 30 (the rest are
object-typed args - announced-types territory). First behavioral run caught a real bug the
unit suite could not: stored procedures took `ParameterMode.All` before the dynamic-bag
Defer check, so proc+bag sites got the parameterless fallback factory ("expects parameter
@ID, which was not supplied") - fixed; ProcedureTests goes 16 failures → 0 bag-related
(the 2 left are the known list-expansion gap).

**New triage item, found by the suite (parity.md §4 said "verify" - now verified as a real
divergence): the First/Single pipeline's CommandBehavior/drain semantics.**
`QueryFirst("select * from #mydata; raiserror(...)")` over 500k rows: vanilla surfaces the
trailing DbException; Dapper.AOT does not (Assert.ThrowsAny: no exception thrown), and the
run burns ~4.5 minutes per provider apparently draining pending rows on reader close. Both
halves point at how the generated First path picks CommandBehavior vs vanilla's deliberate
choices (`Settings.UseSingleRowOptimization` exists precisely because of this trap).

## Round 7b: confirmation run with the proc-mode fix

**612 passed / 150 failed** (down from 170; the proc-mode fix recovered 20). Every remaining
failure class maps onto a planned phase-3 feature: ParameterTests ×23/provider (tokens:
list expansion, TVPs, custom params), TypeHandlerTests ×16 (type-handler story), MiscTests
×16 (coercions + tokens), Async/Literal (literals), plus the First-pipeline drain pair and
the small tail. Nothing unexplained.

## Scoreboard (to update as things land)

| leg | possible (honest) | handled | compiles | tests green | AOT publish |
| --- | --- | --- | --- | --- | --- |
| net10.0 | 725 honest | 533 (73.5%) | ✅ | 612/762 (150 fail, runtime-only) | — |
| net8.0 | > 387 | 387 claimed | ✅ | — | — |
| net481 | > 405 | 405 claimed | ✅ (needs PR #184) | — | — |

All three legs compile as of 2026-08-18 evening, with PRs #180–#184 (the net481 leg — EF
spatial, Linq2Sql — was the finder for #184: `DbGeography`/`Binary` result types emitted
uncompilable construction; now refused with the new DAP050) plus the two harness severity
downgrades. Environment note: **SQL Server 2022 Developer is installed and running locally**
(default instance, matching the suite's default `Data Source=.` connection string), so the
vanilla control run needs no docker.

## Round 2 (same day): both bugs fixed (PRs #180, #181), repack, re-measure

Predictions held: the zero-analyzer-diagnostics anomaly was cascade from Bug A — with the
parse wreck gone the full picture appeared, and the scorecard moved honestly (396 → **394 of
394**: the refused sites left the denominator too, which is the dishonesty already on
record).

Warnings: DAP027 ×402, DAP028 ×44, DAP012 ×40 (tuple-name guidance), DAP018 ×26 (params not
detected in SQL), DAP048 ×16 (DbString→DbValue). Remaining build breaks, i.e. the next
bug/gap batch:

- **types nested in generic classes leak `TProvider`** (CS0246 ×16 + CS0122 ×8):
  `ParameterTests<TProvider>.IntCustomParam` renders with the open type parameter into the
  generated (non-generic) scope. `InvolvesGenericTypeParameter` has the same blind spot
  `IsPublicOrAssemblyLocal` had (PR #180): it walks containment but not type arguments.
  Same fix shape; note the accessibility check *also* missed these (private nested), so
  check ordering/coverage while there;
- **CS1503 ×2**: generated code passes `IDataReader` where `DbDataReader` is required
  (`GetRowParser`-adjacent, generated line 497) — emit bug, uninvestigated;
- **DAP037 as a build error on scalar-ish results** (×16): `Query<AnEnum>`, `Query<char>`,
  `Query<ShortEnum>`, `Query<int[]>` (renders as `''`), `Query<dynamic>` — all fine in
  vanilla Dapper, all "no settable members" *errors* here. The generator lacks scalar
  result-type handling for enums/char/arrays and mishandles `dynamic` as a generic argument;
  an analyzer **error** on a vanilla-supported shape also breaks the build outright, which
  is worth revisiting as a severity question independent of the feature gap;
- **DAP036 as a build error** (×4): `Query<TimeSpan>`, `Query<SqlDecimal>` — BCL structs as
  results hit the constructor-ambiguity error. Same family as above.

The scalar-result gaps are phase-3 features (they add parse-time state); the `TProvider`
leak is a phase-1 refusal fix; the CS1503 needs sizing.
