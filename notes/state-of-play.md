# State of play

**Read this first after a break.** The other notes describe *designs*; this one describes
*where the work currently is* - which branch, which PR, what lands next, and in what order.
Keep it current: it is the cheapest thing to update and the most expensive thing to lose.

Last updated 2026-09-11.

## Where things stand

Phases 1 and 2 of [plan.md](plan.md) are done and merged. Phase 3 (close the gaps, one feature
per round, each verified by a DB-backed run) is in progress; see
[harness-baseline.md](harness-baseline.md) for the round log and the current numbers.

**1.1.0 shipped 2026-09-11** - the first release since 1.0.52 (May), and the first to publish via
release.yml rather than by hand. See "Releasing" below for the procedure and its two traps.

Last measured baseline (**round 15, 2026-09-11**, Linux rig): **729 passed / 800** on the Dapper
suite with **432 of 736** call-sites handled; the vanilla control on the same box is 770/800.
All 41 divergences are known gaps, ×2 providers - no new failure class.

Note the call-site count is **not** comparable with the 533/725 recorded at round 12: that rig
was Windows-only and is gone, and the round-12 generator reads 432 here too. Compare within a
rig, never across. See [harness-baseline.md](harness-baseline.md) round 15.

## In flight

**Nothing of ours.** The type-handler arc is closed out and merged; there is no open PR from
this workstream, and no branch waiting to be pushed.

Landed since the last revision of this page:

| PR | what |
| --- | --- |
| #208 | declarative type handlers: `[TypeHandler(typeof(V), typeof(H))]`, `IDbValueHandler<T>`, the vanilla-handler shim |
| #209 | the never-implemented `[TypeHandler<,>]` pair obsoleted as a warning, not an error |
| #210, #213 | parity notes: the type-handler decision, and statuses now *cite* the generated surface report rather than re-asserting it |
| #211 | DAP000 separates refused-with-diagnostics from skipped-silently |
| #214 | `Type`-based APIs become an explicit non-goal (DAP056) |
| #215, #216 | docs refreshed for .NET 10; interceptor goldens made OS-neutral, and golden write-back fixed |

Open PRs are all external and all await a decision only Marc can give: **#167** (SplitOn - the
prior art for multi-map), **#153** (`CommandDefinition` overloads - speaks directly to the mute
overloads below), **#151** (record primary constructor), **#84** (npgsql-rewrite, 2023).

## The type-handler position, settled

Marc's call, 2026-08-23, unchanged: **keep vanilla's *call* API; we are not obliged to mirror
its *config* API.** Static, build-time registration is the supported path.

**#206 was closed on 2026-08-25**, and that sharpened the position from "runtime registration is
an opt-in migration mode" to **not supported at all** - not on principle, but because even gated
it could not be made free for the people not using it. The reasoning, so it is not re-derived:

- the opt-in stopped it *emitting* anything for consumers who did not ask, but the read-side
  check still lived in `RowFactory.GetValue<T>` - the type-flexible arm of every mapped member,
  ~130 call sites across the interceptor goldens, taken whenever a column type does not exactly
  match the member type. That path gained a `DBNull` type test it never had, a `typeof(T)`
  materialisation and a static delegate probe, and stopped being a single-expression method that
  inlines into generated code. Everyone paid for a feature almost nobody would enable;
- the fix considered and **rejected**: push the check behind the opt-in at *emission* time (as
  the write side already did), by emitting `GetValueViaTypeHandler<T>` only for opted-in
  compilations and giving whole-type handlers their own row factory. That works, and would have
  made the residue one static-field test per query - but it buys a second read path to maintain
  in the runtime library, for a mode that is explicitly temporary and cannot be published under
  native AOT anyway.

The standing reasons, which predate #206 and outlive it:

- runtime registration reaches `SqlMapper.TypeHandlerCache<T>` - a generic instantiated over a
  runtime-chosen type - which ILC cannot resolve and nothing warns about at publish. Issue #165
  is that crash on a deployed app;
- it keeps the world open, so nothing reachable from the registry can be trimmed;
- every runtime knob respected is a permanent per-operation cost - the enum gate in #206 was the
  worked example on the write side, and the `GetValue<T>` residue was the one that killed it;
- generated code baked its decision at compile time, so a later registration is either ignored
  or forces a per-operation check. There is no third option;
- only the static form can be checked at build.

Attribute spelling is **non-generic, `typeof`-based**: on .NET Framework, `GetCustomAttributes()`
throws `NotSupportedException` for the whole call when an assembly or type carries a generic
attribute, poisoning unrelated reflection. Same wall protobuf-net hit, same resolution. Probed
directly; do not re-open this.

The rule worth keeping, should this come up again: *an attribute lands with the behavior it
gates* - never ship an attribute into a PR where it would do nothing.

**Diagnostic ids as shipped**: **DAP053** = runtime registration with no declarative counterpart;
**DAP054** = registration naming a handler generated code cannot use; **DAP055** = duplicate
registration for one type; **DAP056** = `Type`-based API (non-goal). Next free: **DAP057**.

Still future work, not gaps: member-scoped `[TypeHandler(typeof(H))]` (removed from #208 rather
than half-implemented; widening `AttributeUsage` and adding a constructor are both non-breaking,
so it stays a future option), enum auto-handlers, and `[TypeMap]`/settings equivalents - per the
declarative-config direction in [typehandler-registration.md](typehandler-registration.md).

## The harness, rebuilt (2026-09-11)

The rig was local-only on the Windows box and did not survive the move; it has been **rebuilt on
this machine** and the step-by-step is now at the top of
[harness-baseline.md](harness-baseline.md) - databases via the Dapper suite's own
`docker compose`, the `SqlServerConnectionString` env var, the local feed, and the
`.globalconfig` severity downgrades that let the build complete.

It is still the `aot-harness` branch of the sibling Dapper checkout and still **local-only, not
pushed**. Two things to know before trusting a number from it:

- `-p:NoWarn=NU1902` is needed on every build: the Dapper repo runs warnings-as-errors and a
  published advisory against a SourceLink dependency otherwise fails the restore. Nothing to do
  with us, and not worth "fixing" in that repo;
- **absolute call-site counts are rig-specific.** The old rig's 533/725 cannot be reproduced here
  and the round-12 generator does not reproduce it either, so the difference is configuration
  that no longer exists. Compare within a rig.

## Releasing (learned the hard way, 2026-09-11 cutting 1.1.0)

The procedure that works:

1. read the version off a **green main run**'s step summary ("Report computed version" in
   dotnet.yml). A PR run reports the `refs/pull/N/merge` number, which is *not* what will ship -
   the step says which it is;
2. create a GitHub Release tagged with exactly that, **unprefixed** (`1.1.0`, not `v1.1.0`);
   `publicReleaseRefSpec` accepts both since the `v?` fix, and every tag this repo has ever cut
   is unprefixed;
3. release.yml verifies tag == computed version and refuses to publish on a mismatch.

Two things that cost time, so they are written down:

- **`NUGET_USER` must be the policy *creator*, not the policy *owner*.** Trusted Publishing
  policies are created from your own nuget.org account with an owner dropdown; choosing the
  `Dapper` org there is what scopes the policy to org-owned packages, but the token exchange
  looks up policies *created by* the username you pass. Passing `Dapper` gives
  `HTTP 401 ... No matching trust policy owned by user`;
- `versionHeightOffset` is a **fixed shift, not a pin**. Every commit that lands on main before
  the tag moves the computed patch, so re-check `nbgv get-version` on main and decrement the
  offset if the target has drifted. Simulate a squash-merge (`git merge --squash` onto a temp
  branch off main) rather than reasoning about it - branch-local numbers are misleading, since
  the repo squash-merges.

Not a real concern, having checked: the per-version "uploaded by" on the Versions tab is visible
only to owners. Anonymously the package page shows **Owners** only, and both packages already
list `Dapper` and `marc.gravell`, so which identity pushes changes nothing a consumer sees.

## What is next, in the order parity.md argues for

0. **say something at the 34 mute overloads** - 22 skipped silently + 12 unsupported-undiagnosed.
   Still the cheapest safety win on the list: it turns a runtime AOT failure into a build warning
   without supporting anything new, and #214 just proved the pattern end to end;
1. **multi-map** (`Query<T1..T7,TReturn>` + `splitOn`) - large; external #167 is the prior art;
2. **`QueryMultiple` / `GridReader`** - large, and needs a Dapper-side extension point first;
3. **corpus adoption of `[TypeHandler]`** - a harness edit, and the thing that closes the
   677-vs-705 gap #206 used to cover: the Dapper suite registers its handlers at runtime, so with
   #206 gone those tests bind without handlers. Declaring them via `[module: TypeHandler(...)]` is
   the fix, in the same spirit as the round-6 DTO restructure. Not all of it converts - see
   parity.md on instance-registering tests;
4. **literal injection `{=name}`** (generator half; the analyzer half shipped as #191);
5. **the coercion tail** - highest silent-wrongness risk.

## Adjacent things not to lose

- **net48 is unverified since #216 and #214.** Both changed interceptor goldens, and that leg
  cannot run on Linux. One Windows run settles it; until then, the `.output.netfx.*` files went
  in on reasoning rather than on a green run.
- **The in-repo `type-handler` branch** (last commit literally "incomplete", 2024-11, off a
  Dec-2023 main) carries the richest handler protocol written so far - `Tokenize` /
  `Parse(reader, ordinal, token)` matching `RowFactory`, plus `EnumTypeHandler<T>` and
  `[EnumString]`. The generator half predates phase 2 and would be rewritten; the library half
  is the asset. This was filed as "harvest before #208 settles" - **#208 has now settled**, so
  the harvest is due, or the branch is a deliberate write-off.
- **External PRs #117 and #162** were the prior art for the static tier; **both are now closed**,
  superseded by #208. Their instance registry and interceptor goldens were the parts worth
  keeping - triage write-ups exist outside this repo.
- **Dapper #2225 and #2228 are merged but unreleased.** Both landed 2026-08-20; the latest
  release is still 2.1.79, from May, so no tag contains them. DapperAOT now pins **2.1.79**
  (bumped 2026-09-11, hygiene only - it changed nothing and unlocks nothing). When a release
  ships *with those two*: bump again, add the DAP052 positive twin and the defer-emit golden,
  and take the `TestUnexpectedDataMessage` parity that was deferred to that bump. The DAP052
  verifier pins 2.1.72 itself (`DAP052.DapperWithoutTheApi`) and is unaffected by either bump -
  that is deliberate, so it keeps guarding the probe-and-refuse path.
