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

- (start) Branch `phase2-model` from `main` (42ad705 + merges). Baseline hash of the harness
  generated file to be recorded below.
