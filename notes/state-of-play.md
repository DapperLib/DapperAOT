# State of play

**Read this first after a break.** The other notes describe *designs*; this one describes
*where the work currently is* - which branch, which PR, what lands next, and in what order.
Keep it current: it is the cheapest thing to update and the most expensive thing to lose.

Last updated 2026-08-25.

## Where things stand

Phases 1 and 2 of [plan.md](plan.md) are done and merged. Phase 3 (close the gaps, one feature
per round, each verified by a DB-backed run) is in progress; see
[harness-baseline.md](harness-baseline.md) for the round log and the current numbers.

Clean-main baseline: **677 passed / 793** on the Dapper suite, **533 of 725** call-sites
intercepted.

## In flight

| PR | branch | what | state |
| --- | --- | --- | --- |
| #206 | `typehandlers` | runtime `AddTypeHandler` registrations honored, behind `[module: UseRuntimeTypeHandlers]` (default off) | draft, rebased on main, tests green |
| #207 | `typehandler-registration-note` | the note explaining why the shipped `[TypeHandler<,>]` never worked, and the agreed route | draft/open, notes only |
| #208 | `typehandler-attributes` | the declarative replacement: `[TypeHandler(typeof(V), typeof(H))]`, `IDbValueHandler<T>`, the vanilla-handler shim, obsoletes | **ready for review**, complete |

**Landing order (revised 2026-08-25): #207 whenever, then #208, then #206.**

The original plan was "#206 first", on the grounds that it was finished and restores the corpus
number. That was wrong for a concrete reason: **#206's `docs/rules/DAP053.md` prescribes
`[module: TypeHandler(typeof(V), typeof(H))]`**, which only exists in #208. Merging #206 alone
ships a diagnostic whose documented fix does not compile. So either #208 goes first, or #206's
rule doc is softened to stop naming an API that is not there yet.

The rule still worth keeping from the original reasoning: *an attribute lands with the behavior
it gates* - `[UseRuntimeTypeHandlers]` belongs to #206, not #208, because shipping it where it
would do nothing is the exact sin these PRs exist to fix.

**#206 is also discardable**, and that is a live option rather than a formality. Given the
position that runtime config need not be mirrored, the only things it buys are a migration path
for existing JIT users and the corpus 705-vs-677. Closing it and declaring handlers in the Dapper
suite instead is coherent: it costs a suite edit and removes a whole opt-in surface from the
public API. #208 alone is a complete story; #206 alone is not.

**Diagnostic ids**, allocated so the two can land in either order: **DAP053** = #206
(`[UseRuntimeTypeHandlers]` + `PublishAot`); **DAP054** = #208 (runtime registration with no
declarative counterpart); **DAP055** = #208 (registration naming an unusable handler). Next
free: DAP056.

## The position these PRs encode

Marc's call, 2026-08-23: **keep vanilla's *call* API; we are not obliged to mirror its *config*
API.** Static, build-time registration is the supported path; runtime registration is a
migration mode, opt-in, and documented as not AOT-publishable.

The reasoning, so it does not have to be re-derived:

- runtime registration reaches `SqlMapper.TypeHandlerCache<T>` - a generic instantiated over a
  runtime-chosen type - which ILC cannot resolve and nothing warns about at publish. Issue #165
  is that crash on a deployed app;
- it keeps the world open, so nothing reachable from the registry can be trimmed;
- every runtime knob respected is a permanent per-operation cost (the enum gate in #206 is the
  worked example);
- generated code baked its decision at compile time, so a later registration is either ignored
  or forces a per-operation check. There is no third option;
- only the static form can be checked at build.

Attribute spelling is **non-generic, `typeof`-based**: on .NET Framework, `GetCustomAttributes()`
throws `NotSupportedException` for the whole call when an assembly or type carries a generic
attribute, poisoning unrelated reflection. Same wall protobuf-net hit, same resolution. Probed
directly; do not re-open this.

## What #208 still needs

Nothing blocking - it is ready for review. Closed since the first draft:

- DAP055 now reports a registration naming something generated code cannot use (was a silent
  skip, which was the failure mode the PR exists to kill);
- `Tokenize` is wired: the handler's per-column token travels in the row factory's `state`
  channel - one int array per query, filled by a second pass over the token span, indexed
  positionally in `Read`. `TypeHandlerProtocolTests` pins the contract from outside the
  generator;
- member-scoped `[TypeHandler(typeof(H))]` was **removed** rather than implemented: the
  attribute was advertising a form nothing reads, which is the same no-op sin. Widening
  `AttributeUsage` and adding a constructor are both non-breaking, so it stays a future option.

Still future work, not gaps in this PR: enum auto-handlers and `[TypeMap]`/settings equivalents,
per the declarative-config direction in [typehandlers-design.md](typehandlers-design.md).

## Adjacent things not to lose

- **The in-repo `type-handler` branch** (last commit literally "incomplete", 2024-11, off a
  Dec-2023 main) carries the richest handler protocol written so far - `Tokenize` /
  `Parse(reader, ordinal, token)` matching `RowFactory`, plus `EnumTypeHandler<T>` and
  `[EnumString]`. The generator half predates phase 2 and would be rewritten; the library half
  is the asset. Harvest before #208 settles.
- **External PRs #117 and #162** are the prior art for the static tier and have been open for
  months awaiting a decision that only Marc can give. #162 is the more complete; its
  generic-attribute spelling is superseded, its instance registry and interceptor goldens are
  not. Triage write-ups exist outside this repo.
- **Dapper #2225 and #2228 are merged but unreleased** (latest release is 2.1.79, from May).
  DapperAOT pins 2.1.72. When a release ships: bump Dapper and Dapper.StrongName, add the DAP052
  positive twin and the defer-emit golden, and take the `TestUnexpectedDataMessage` parity that
  was deferred to that bump.
