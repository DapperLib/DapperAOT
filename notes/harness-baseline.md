# Harness baseline: Dapper.AOT enabled in the Dapper test suite

First real numbers, 2026-08-18. Setup lives on the `aot-harness` branch of the **Dapper**
repo (sibling checkout): local package feed at `../DapperAOT/artifacts` (pack with
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

So the corpus number to drive to 100% starts at **53%**. The 82 unsupported-API sites are the
`QueryMultiple`/multi-map/`ExecuteReader` families (parity.md §1); the 256 diagnostic skips
are dominated by the DAP012/DAP015/DAP016/DAP017/DAP050 shapes plus untyped parameters —
harvesting that breakdown per-id into the audit table is the remaining phase-1 analysis step.

## Scoreboard (to update as things land)

| leg | possible (honest) | handled | compiles | tests green | AOT publish |
| --- | --- | --- | --- | --- | --- |
| net10.0 | > 387 (denominator understated) | 387 claimed | ✅ | — | — |
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
