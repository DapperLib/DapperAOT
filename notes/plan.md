# The plan

Agreed 2026-08-18. Three phases, in order. The rule that resolves the phase-1/phase-2
tension: **nothing that adds parse-time state lands before phase 2 completes** — refusals
and scorecard changes are fine, features are not.

## Phase 1 — complete the gap table (empirically, not by reading)

The parity table's remaining ❓s are resolved by the harness, which is currently blind. So
phase 1 includes the minimum generator work needed to make the instrument read:

- fix **Bug A** (array-of-anonymous parameter emits the display string and wrecks the parse)
  and **Bug B** (inaccessible types emitted rather than refused with DAP017) — see
  [harness-baseline.md](harness-baseline.md). Both are parse/refusal decisions that survive
  the phase-2 model rework, which is why they are allowed ahead of it;
- fix the **DAP000 scorecard honesty**: count *every* Dapper call-site, split
  handled / unsupported (with ids) / failed. That scorecard is the gap table's data source,
  so it is phase-1 work, not phase-3;
- re-run the harness, confirm the analyzer diagnostics appear once the compile is clean
  (the zero-DAP001 anomaly), harvest the flood, and replace the guessed rows in
  [test-suite-audit.md](test-suite-audit.md) and the ❓s in [parity.md](parity.md) with
  measured ones;
- in parallel: stand up SQL Server (docker or Developer Edition — both fine on this
  machine) and get the **vanilla** suite green as the control, so the behavioral instrument
  exists before phase 3 needs it.

Exit criteria: suite compiles with AOT on; honest scorecard numbers for all three TFM legs;
parity table has no ❓ that the harness could have answered.

## Phase 2 — fix the generator capture model

[generator-audit.md](generator-audit.md) is the spec. Requirement: **byte-identical
generated output before/after**, checked two ways for free — the interceptor golden
fixtures (`*.output.cs`), and a diff of the harness's emitted `Dapper.Tests.generated.cs`.

The same change adds the **shape-enforcing reflection test** (no Roslyn reference types in
the cached model, Roslyn value types allowed) — the constraint has to have teeth in CI or
it erodes again; the test landing with the fix is what made the rule stick in protobuf-net.

Exit criteria: model is plain equatable data; `CompilationProvider` no longer feeds the
output step; shape test in CI; output byte-identical.

## Phase 3 — close the gaps, in the sequenced order

Order as drafted in [test-suite-audit.md](test-suite-audit.md): DynamicParameters → token
rewrites (list expansion / literals / pseudo-positional) → QueryMultiple/GridReader →
multi-map → announced types → type handlers → the tail. Revisit the order once phase 1's
measured numbers land — it was drafted from estimated impact.

**Definition of done per feature**: compiles + intercepted + **DB-backed tests green**, not
just build-time clean — the test run is the only net that catches silent divergence (see
the measurement caveat in [test-suite-audit.md](test-suite-audit.md)).
