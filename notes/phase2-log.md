# Phase 2 log: generator capture-model rework

Running log, newest entries at the bottom. Written as-I-go (sessions can be lost); decisions
that need after-the-fact review are marked **[DECISION]**. The spec is
[generator-audit.md](generator-audit.md); the safety net is byte-identical generated output
(golden fixtures + a hash of the Dapper suite's generated file).

## Approach

The full job — plain equatable model, no `CompilationProvider` into the output step, shape
test in CI — is multi-session. Order of attack, chosen so every increment is independently
verifiable and shippable:

1. **Baseline**: hash the harness's `Dapper.Tests.generated.cs` off current `main`; goldens
   are already in git. Any increment that changes either is a bug (or a consciously recorded
   exception).
2. **`TypeAccessorInterceptorGenerator` first** — the small generator (state: `Location` +
   `ITypeSymbol` + `IMethodSymbol`), converted end-to-end to prove the pattern cheaply.
3. **Interceptor generator, by axis**, each a separate commit with the byte-identical check:
   a. `Location` → span-based plain struct (also needed: project the interceptor file path at
      parse time, since emit currently asks the `SourceTree` for it);
   b. `IMethodSymbol Method` → projected signature model (emit only needs text: return type,
      parameter list, name, arity; grouping needs equality, which becomes string equality);
   c. `ResultType`/`ParameterType`/`AdditionalCommandState` → the big projection: everything
      `WriteRowFactory`/command-factory emission reads from symbols (members, db types,
      nullability, constructor choice) moves to parse-time plan data. This is the protobuf-net
      "plan model" equivalent and is the bulk of the work.
4. **Remove `CompilationProvider` from the output step** — what Generate still needs from the
   compilation (DbCommand type discovery, `AllowUnsafe`, language version) gets its own
   projected provider(s) with equatable outputs.
5. **Shape test** (reflection over the model namespace, no Roslyn reference types allowed)
   lands with the increment that completes 3c, and gates CI from then on.

**[DECISION]** Two-stage risk framing: the *retention* harm (cached values pinning
compilations) is fixed by 2-3; the *recompute* harm needs 4 as well. If the session ends
mid-way, 2-3 alone are still worth merging — a full-recompute generator without the leak
beats today's state.

## Log

- (start) Branch `phase2-model` from `main`. Baseline hash of the harness generated file:
  `f8d3a61f81455c8ef0bcecccbfc348a1584a6bab` (copy kept at `/tmp/baseline-generated.cs` for
  diffing; regenerate with `-p:EmitCompilerGeneratedFiles=true
  -p:CompilerGeneratedFilesOutputPath=obj/gen` — under `obj/`, or the default glob compiles
  the stale copies as source, which cost an hour earlier).
- **Increment 1 done** (pushed on `phase2-model`): `TypeAccessorInterceptorGenerator`
  converted end-to-end. New `Dapper.CodeAnalysis.Model` namespace: `LocationSnapshot`,
  `EquatableArray<T>`, `TypeAccessorModel`/`AccessorMember`/`ForwarderMethod`,
  `GenerationEnvironment` (AllowUnsafe + AssemblyName + has-InterceptsLocationAttribute — the
  only three facts the output step needed from the `Compilation`). `ModelShapeTests` enforces
  no-Roslyn-fields + IEquatable over the namespace by reflection. Byte-identical: Accessors
  goldens unchanged; full suite green net10/net48.
  **[DECISION]** `PreGeneratedCodeWriter` keeps its `Compilation` ctor alongside the new
  bool one until the big generator converts — avoids touching it out of order.
  **[DECISION]** `ForwarderMethod`/type-name projection replicates `CodeWriter.Append(ITypeSymbol)`
  exactly (anonymous → MinimallyQualified, else GetTypeName) so output cannot shift.
- **Increment 3a done** (pushed): `Location` out of all interceptor-generator cached states.
  `LocationSnapshot` gained MappedPath/MappedStartLine (the IncludeLocation SQL comment used
  `GetMappedLineSpan`); the interceptor file path and language version are projected at parse
  (emit used to reach through `Location.SourceTree` for both). Byte-identical: goldens
  unchanged + harness hash equal (`f8d3a61f`).
  **[NOTE]** the Bash harness un-escapes backslashes in commands, which broke two heredocs
  before being identified — long edit scripts now go via a file, not a heredoc.
- **Increment 3b done** (pushed): `IMethodSymbol Method` out of the cached state -
  `InterceptedMethod`/`MethodParam` plain models; grouping by structural equality;
  `CodeWriter.GetAppendTypeName` added as the canonical projection helper. Byte-identical
  both ways again (`f8d3a61f`).
- Remaining in the cached model: `ITypeSymbol? ResultType`, `ITypeSymbol? ParameterType`,
  and `AdditionalCommandState` (which reaches MemberMap: symbols + an IOperation). That is
  increment 3c - the big projection (row factories, command factories, member maps) - plus
  4 (CompilationProvider out of the output step: DbCommand type discovery, AllowUnsafe,
  assembly name, pre-generated helpers). 3c is protobuf-net-plan-scale work; the shape test
  already guards everything in the Model namespace, and 2+3a+3b are independently mergeable
  if the session ends here.
- **Increment 3c-i done** (pushed): `AdditionalCommandState`/`CommandProperty` plain and
  moved into the Model namespace (now under the shape test); member-exists and is-DbCommand
  probes run at parse; QueryColumns/CommandProperties on `EquatableArray`, which had to learn
  default-vs-empty (QueryColumns semantics distinguish them). Byte-identical again.
  **[CORRECTION]** an earlier chat message called QueryColumns equality a latent reference-
  equality bug - wrong: `AdditionalCommandState` had proper element-wise static helpers; the
  conversion is for shape-consistency, not a bug fix.
  **[NOTE]** `HasPublicSettableInstanceMember` returns true for *readonly* fields
  (`field.IsReadOnly` looks inverted) - preserved as-is for byte-identical output; flag for
  a separate look.
- Remaining cached symbols after 3c-i: exactly two - `ITypeSymbol? ResultType` and
  `ITypeSymbol? ParameterType` on `SuccessSourceState`. Everything else in the cached model
  is plain. Those two feed the row-factory/command-factory emitters, which consume symbols
  deeply (MemberMap/ElementMember); projecting them is the protobuf-net-plan-scale increment
  (3c-ii), best started fresh rather than at a session tail.
- **Increment 3c-ii result side done** (pushed on stacked branch `phase2-model-plans`;
  checkpoint PR #187 covers everything before it): `ResultType` → `RowPlan`/`RowMember` -
  the full row-factory projection (member types/db-names/reader-methods, ctor/factory
  choice, deferred construction, inbuilt-helper detection, query-column mapping).
  `RowReaderState` now de-dupes on plan equality. Byte-identical (`f8d3a61f`) on first run.
  **One cached symbol remains in the entire model: `ParameterType`** - the command-factory
  side (WriteCommandFactory ~450 lines: member accessors, DbType/size/direction facts,
  anonymous shape witness, multi-exec element/cast, UnsafeAccessor-adjacent bits). Same
  recipe; roughly twice the surface of the row side.
- **Increment 3c-ii parameter side done** (pushed): `ParameterType` → `ParamPlan`/`ParamMember`
  (command-factory emission incl. Add-mode sizing decisions, DbString/cancellation, the
  anonymous shape witness, multi-exec element plan). `CommandFactoryState` keys on plan
  equality. **Zero Roslyn objects remain in the cached model.** The states also gained
  structural equality (reference equality was still defeating the node-level cache), and the
  shape test now covers the SourceState family. Byte-identical throughout (`f8d3a61f`).
- **Remaining for phase 2**: increment 4 only - the interceptor generator's output step still
  combines the raw `CompilationProvider` (used for: DbCommand type discovery/
  GetSpecialCommandFlags, GetCommandFactory, AllowUnsafe, assembly name, PreGeneratedCodeWriter,
  and the CommandFactoryState systemObject plan). Project those into an equatable environment
  (the TypeAccessor generator shows the pattern) and the recompute half is done too.
- **Increment 4 done — PHASE 2 COMPLETE** (PR #188, stacked on #187): `InterceptorEnvironment`
  projected via `Select` (AllowUnsafe, assembly name, InterceptsLocationAttribute presence,
  the DbCommand special-types sweep pre-filtered, module-level `[CommandFactory<T>]`, the
  object fallback plan); the analyzer bridge builds the same environment. All four exit
  criteria met: plain equatable cached model (zero Roslyn objects, shape-test-enforced
  including the SourceState family), no CompilationProvider into either output step, shape
  test in CI, output byte-identical (`f8d3a61f` unchanged across the entire rework; goldens
  green net10/net48 at every increment).
- Review pointers for the PRs: the states now have structural equality *as well as* plain
  fields — both halves were needed for node-level caching; the `HasPublicSettableInstanceMember`
  readonly-field quirk is preserved-as-was and flagged; ordering of the DbCommand special
  types follows the same HashSet enumeration as before (projection just moved it earlier).
- **Incremental-caching tests added** (on #188, at review request): three driver-tracked
  cases - unrelated-file edit → all output steps Cached; same-file edit *below* the
  call-site → cached (this is the case that specifically needs the states' structural
  equality, since Parse re-runs and yields fresh instances); real shape edit → re-runs and
  output changes. Finding while writing it: editing the SQL *literal* does not change the
  generated text (SQL flows through as an argument), so the "real edit" probe must change
  shape, not SQL.
- **The readonly-field quirk is fixed** (PR #190, stacked on #188, approved by Marc): the
  `[CommandProperty]` member probe said readonly fields were assignable and mutable ones were
  not; now inverted to sense, with a theory covering all five member shapes (the two field
  cases fail without the fix). Kept out of the rework PRs deliberately - behavior change vs
  byte-identical contract. Review stack: **#187 → #188 → #190**.
- Next was: increment 3a — `LocationSnapshot` into the interceptor generator's `SuccessSourceState`
  (plus projecting the interceptor file path at parse, since emit asks the `SourceTree` for it).
