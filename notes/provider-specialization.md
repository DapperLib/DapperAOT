# Provider specialization

A performance direction for generated code: **emit against the concrete provider the consumer
already references**, instead of against the provider-agnostic ADO.NET surface.

This note is the case for it, the specific techniques, and how it would be sequenced. It is
independent of the parity work in [plan.md](plan.md) — parity is about *what* we can intercept,
this is about *what we emit* once we have — but it shares the same success condition, in that
anything here must keep the acceptance corpus green.

## The opportunity, measured

Allocation per operation, PostgreSQL 17 over loopback, two workloads: a single-row primary-key
lookup and a 100-row scan, materialising a three-column POCO (`int`, `string`, `double?`).

(The Dapper.AOT row is the **default** configuration. `[CacheCommand]` changes it — see below.)

| stack | single row | ratio | 100 rows | ratio |
| --- | ---: | ---: | ---: | ---: |
| vanilla Dapper | 1,888 B | 1.00 | 17,995 B | 1.00 |
| **Dapper.AOT, default** | **1,927 B** | **1.02** | **14,143 B** | **0.79** |
| hand-tuned ADO.NET | 1,035 B | 0.55 | 11,311 B | 0.63 |
| no ADO.NET at all | 943 B | 0.50 | 11,207 B | 0.62 |

Two readings, and the second is the one that matters:

- **Dapper.AOT today → hand-tuned ADO.NET is a 46% cut on the single-row path**, 20% over a hundred
  rows. All of it reachable from generated code, within the existing contract.
- **hand-tuned ADO.NET → bypassing ADO.NET entirely is a further 9%**, and 1% over a hundred rows.
  So the ceiling is close: there is little left beyond what the current surface permits, and most of
  the available win is in front of us rather than behind an API redesign.

The last row is a research prototype that speaks the PostgreSQL wire protocol directly with no
`DbCommand`, `DbParameter` or `DbDataReader` anywhere in the path; it is here only to show where the
ceiling is.

**Timings are in the follow-up section below, and they matter more than this table does.** An
earlier draft of this note claimed the wall-clock "says nothing" because a ~475 µs round trip
dominates. That was wrong, and it was a conclusion drawn from a badly configured instrument — the
runs behind it used BenchmarkDotNet's *short* job, three warmup and three iterations, which is fine
for allocation (counted, not timed) and useless for timing. With a normal job the error bars fall to
±5-8 µs and the differences resolve cleanly. The constant round trip is in fact what *helps*: it is a
fixed offset, so the difference between two stacks is the work they do.

**Dapper.AOT shows no single-row allocation win over vanilla here (1,927 vs 1,888), and that is now
explained**: this is the default configuration, which neither re-uses nor prepares commands.
`[CacheCommand]` recovers part of it and preparation is unclaimed entirely — see the two measured
sections below. The comparison to make is therefore against `[DapperAot, CacheCommand]`, not against
the default.

## What "hand-tuned" actually did

Seven techniques, all ordinary ADO.NET, none exotic. This is the specification of what generated code
should aim to emit.

1. **One command, created once, reused across executions.** Not a command per call. The
   `AddParameters`/`UpdateParameters` split already exists for exactly this; the question is whether
   the cache engages in practice ❓.
2. **`PrepareAsync()` on that command, once.** `CanPrepare` is already emitted as `true` for eligible
   factories. Preparation moves parse and plan off the per-execution path.
3. **A generic, provider-specific parameter.** `NpgsqlParameter<int>` carries a `TypedValue` of type
   `int`. The generic surface is the whole point: `DbParameter.Value` is `object`, so every value type
   assigned through it **boxes, on every execution**. This is the largest single item — and, per the
   table below, currently available on Npgsql alone.
4. **No `DbType` inference per assignment.** With a provider parameter the type is fixed by
   construction; with the generic surface, setting `Value` can trigger type inference inside the
   setter.
5. **Typed getters on the concrete reader.** `NpgsqlDataReader.GetInt32(0)` rather than `GetValue(0)`
   or the indexer, both of which return `object`. Dapper.AOT already emits typed getters against
   `DbDataReader`; the specialization is emitting them against the concrete reader type so the calls
   are non-virtual candidates.
6. **`CommandBehavior.SequentialAccess`**, plus `SingleRow` where the call site's arity says so
   (`QuerySingle`, `QueryFirst`). Arity is known at the call site and is currently thrown away.
7. **`IsDBNull(i)` then a typed get**, rather than fetching as `object` and testing for `DBNull`.

Items 3 and 4 are the ones that need provider knowledge. The rest are provider-agnostic and could
land first — see the sequencing below.

## Why this is a generator's job specifically

A runtime library that wants `NpgsqlParameter<int>` has three options, and all of them are bad:

- **reference every provider** — impossible for a library with Dapper's reach, and a dependency
  graph nobody would accept;
- **reflect, or emit IL at runtime** — the thing we are retiring, and AOT-hostile besides;
- **go without** — which is where vanilla Dapper is, necessarily.

A generator has none of these problems. It emits *source*, compiled against whatever the consumer
already references, with no runtime type discovery, no added package dependency, and no cost paid by
consumers who do not use that provider. If the reference is not there, the code that mentions it is
simply not emitted.

That is a structural capability rather than a convenience, and it is the strongest available argument
for doing this in Dapper.AOT rather than anywhere else.

## Detecting the provider

The static type at the call site proves it when it is a concrete provider type
(`NpgsqlConnection.QuerySingle<T>(...)`), and that is the easy case: specialize, no test.

**It is not the common case.** A great deal of real code takes `DbConnection` or `IDbConnection`
from DI, and a rule of "specialize only when the static type proves it" would decline most of the
codebases this is meant to help. So where the static type does not prove it, and the consumer
*references* a provider we specialize for, emit a runtime type test with the agnostic path as the
`else`:

```csharp
if (cmd is NpgsqlCommand npgCmd) { /* specialized */ }
else                             { /* exactly what is emitted today */ }
```

Three details that decide whether this is sound:

- **test the command, not the connection.** `NpgsqlParameter<T>` needs an `NpgsqlCommand`, and
  `cnn.CreateCommand()` is typed `DbCommand` whatever the connection is. Testing the thing about to
  be used is one test either way and does not rely on inferring one type from another;
- **wrapped connections fall out correctly, and that is a feature.** MiniProfiler, OpenTelemetry-style
  decorators and any other wrapping command fail the test and take the agnostic arm, keeping exactly
  today's behaviour. Worth saying out loud, because "does this break my profiler" is the first
  question someone will ask;
- **the test is free in context.** One type check against a database round trip, and monomorphic at
  each call site, so it predicts perfectly.

The remaining rules:

- **detect by symbol resolution, never by package name or version.** `NpgsqlParameter<T>` either
  resolves in this compilation or it does not; that is the only question that matters, and it makes
  version skew a non-issue rather than a support matrix;
- **the reference set decides which arms exist.** No Npgsql reference, no Npgsql arm — it would not
  compile anyway. Most codebases reference one provider; some reference two (SQL Server plus SQLite
  for tests). **Cap the number of specialized arms** rather than emitting an open-ended cross
  product; past the cap, emit agnostic only ❓ (the cap wants picking against real codebases);
- **fall back silently.** An unrecognised provider, or a recognised one whose specialized types do
  not resolve, emits exactly what is emitted today. Specialization is an optimisation, never a
  behavioural change, and never a build break.

## Per-provider surface

**The generic-parameter question decides how far item 3 travels, and the answer is: not far.**
Checked by scanning the shipped assemblies for a generic `*Parameter\`1` type, across every version
in the local package caches:

| provider | versions checked | generic parameter | concrete reader |
| --- | --- | :-: | --- |
| Npgsql | 5.0.0 → 10.0.2 | ✅ `NpgsqlParameter<T>`, with `TypedValue` | `NpgsqlDataReader` |
| Microsoft.Data.SqlClient | 1.1.3 → 7.0.1 | ❌ none | `SqlDataReader` |
| MySqlConnector | 1.1.0 → 2.5.0 | ❌ none | `MySqlDataReader` |
| Microsoft.Data.Sqlite | 5.0.0 → 10.0.11 | ❌ none | `SqliteDataReader` |

So **avoiding the parameter box is a PostgreSQL-only win today**. For the other three, a value-type
parameter must pass through `DbParameter.Value`, which is `object`, and boxes on every execution —
there is no supported way around it from outside the driver.

Two consequences worth taking seriously:

- **the prize is provider-dependent.** On Npgsql the plan is items 1–7. On the others it is items 1,
  2, 5, 6 and 7, and the measured 46% should not be assumed to transfer — it needs measuring per
  provider before it is quoted ❓;
- **this is a well-evidenced feature request for those drivers.** "Add a generic parameter type"
  backed by a measured allocation delta is a much better conversation than an abstract one, and it
  costs nothing to have. If any of them took it, item 3 would light up for their users with no
  further work here.

The concrete readers are available everywhere, so items 5 and 6 specialize for all four.

## The major: emit end-to-end method bodies

The larger option, and the one that makes everything above straightforward rather than fiddly:
**stop emitting factories for runtime plumbing to drive, and emit the whole operation as a method**
— create or reuse the command, set parameters, execute, loop, materialise, dispose — with the
runtime library reduced to helpers.

### The shape

Two layers. The interceptor is per call site and does almost nothing; the body is shared.

```csharp
// interceptor: per call site, tiny, and the only place the anonymous type is in scope
[InterceptsLocation(...)]
internal static Task<Customer> Intercept_7(this DbConnection cnn, string sql, object? param, ...)
{
    var typed = Cast(param, static () => new { id = default(int) });
    return Shapes.QuerySingle_Customer_id_Int32(cnn, sql, typed.id, ...);
}

// shared: one per (shape, provider). No anonymous type in the signature, so it is nameable.
internal static async Task<Customer> QuerySingle_Customer_id_Int32(
    DbConnection cnn, string sql, int id, ...)
{
    var cmd = cnn.CreateCommand();
    if (cmd is NpgsqlCommand pg) { /* NpgsqlParameter<int>, typed getters, prepared */ }
    else                        { /* exactly today's agnostic path */ }
}
```

**If the body is factored out, it must take the extracted values rather than the argument object** —
that part is forced, not chosen. An anonymous type cannot be written as a parameter type, so a helper
taking one is unwritable; passing `typed.id` sidesteps it.

### ...or just put the body inline

The obvious alternative is to drop the second layer entirely: emit the body directly under
`var typed = Cast(...)` in the interceptor and use `typed.id` from there. **It runs the same code**,
and the specialization switch works identically. So the choice is narrower than it first looks, and
worth stating honestly rather than assuming the factored form wins:

| | inline in the interceptor | shared body per shape |
| --- | --- | --- |
| per-operation cost | same | same |
| async state machines | **one type per call site** | one type per shape |
| emitted source, compile time, golden fixtures | N near-identical bodies | one body |
| debugging | N places | one place |
| generator complexity | lower — no shape key, no signature to design | higher |

The async state machine row is the most concrete: an inline body makes the interceptor `async`, so
each call site carries its own state machine type, with the metadata and the ILC work that implies.
Delegating lets the interceptor be a non-async `return Shapes.X(...)`, and the shape has one.

**Whether that is worth the machinery depends entirely on how often shapes repeat**, and that number
is unmeasured ❓. An earlier draft of this note asserted that fifty call sites would collapse to one;
that was illustration presented as fact. If a real codebase repeats a given (operation, row type,
parameter types) two or three times, the saving is modest and the inline form's simplicity probably
wins. **Measure shape repetition across a few real repositories before choosing.**

If the factored form is chosen, the shape key is (operation, row type, parameter names and types),
so differing SQL is free — it is a parameter — and differing parameter *names* is what splits a
shape, unless names are passed too ❓.

### Passing the binder in, rather than baking it in

A variant worth recording, because it fixes the sharing problem the two forms above both have. Rather
than the shared body taking the extracted values — which puts the parameter types in its signature —
pass the arguments as `object` plus a **binder** that knows how to destructure them:

```csharp
internal static async Task<Customer> QuerySingle_Customer(
    DbConnection cnn, string sql, object args, Action<DbCommand, object> bind, ...)
```

**That erases the argument shape out of the shared body entirely**, so the shape key collapses from
*(operation, row type, parameter names and types)* to just *(operation, row type)*. Every
`Query<Customer>` in a codebase shares one body regardless of what it passes. A post-process binder
for output parameters fits the same way.

Function pointers are the obvious primitive, and measurement does not support them:

```
                                 alloc/call   cost/call
inline body                        24.0 B      1.94 ns
via delegate* parameter            24.0 B      7.09 ns   (+5.15)
via static readonly delegate       24.0 B      4.04 ns   (+2.10)
```

- **neither indirection allocates.** A `static readonly` delegate is built once at type init, so the
  per-call cost is zero either way — the 24 B is the caller's argument object in all three;
- **the function pointer measured *slower* than the delegate**, most likely because the JIT can
  speculatively inline through a delegate with a stable target and cannot do the same for a pointer
  arriving as a parameter ❓ (one microbenchmark, and a trivial body exaggerates call overhead — but
  enough to retire "pointers because they are faster");
- **and it is all noise at the scale that matters.** 2-5 ns against a ~475 µs round trip is ~0.001%.
  Choose on code size and generator complexity, not speed.

So **the delegate form is the better default**, not the fallback: it needs no `AllowUnsafeBlocks`, no
detection of it, and no second emission path. Worth knowing that function pointers *would* impose
that — a consumer without `AllowUnsafeBlocks` gets **CS0214** from generated code, and that is a
compilation-wide switch a generated file cannot opt into on its own.

One simplification found while checking: **the binder does not need a per-provider signature.**
Function-pointer parameters are contravariant, so narrowing is rejected outright (`CS8757`) and a
`delegate*<NpgsqlConnection, ...>` would force a separate shared body per provider. Unnecessary — the
binder can take `DbCommand` and cast internally, because the shared body has already proven the type
in its `if (cmd is NpgsqlCommand)` branch. One binder signature serves every provider.

The cost of this variant is that the binder is an opaque call in the middle of the body, so the JIT
cannot optimise across it the way it can for a fully inline body. Against a database round trip that
is irrelevant; it is recorded because it is the one real difference.

### Why it is worth a major

- **it removes a type-erasure tax the current design imposes.** An anonymous type cannot be named as
  a type argument on a runtime type, so the factory for `new { id = 42 }` degrades to
  `CommandFactory<object?>` and casts back:

  ```csharp
  private sealed class CommandFactory0 : CommandFactory<object?> // <anonymous type: int id>
      var typed = Cast(args, static () => new { id = default(int) });
  ```

  The erasure costs a `castclass` rather than an allocation — see the closed item below — but it also
  forces the *shape* of everything downstream;
- **provider specialization stops being a plumbing problem.** With factories it needs a
  provider-specific factory hierarchy or generic gymnastics; with an emitted body,
  `new NpgsqlParameter<int> { TypedValue = id }` is a line and the detection switch is an `if`;
- **the optimisation surfaces become local and readable.** Every allocation on the path is in
  generated source that can be read and diffed, rather than distributed across a library the
  generator cannot see into. The step-0 anomaly is exactly the kind of question that becomes
  answerable by reading instead of profiling.

**And the payoff is already estimated.** The "hand-tuned ADO.NET" row in the opening table is
essentially what this emits — command created once and reused, prepared, typed parameter, typed
getters, no factory indirection, no erasure. So **1,927 B → 1,035 B is the estimate for this
proposal specifically**, not a general aspiration.

### Validating the shape (measured)

The shape above was written out by hand — interceptor-constrained entry, binder in a
`static readonly` delegate, shared body with the provider type test — and measured against the same
PostgreSQL workload as the opening table. Single-row lookup:

| stack | allocated | vs Dapper.AOT |
| --- | ---: | ---: |
| Dapper | 1,865 B | |
| Dapper.AOT | 1,971 B | — |
| **emitted shape, fresh command per call** | **1,848 B** | -6% |
| **emitted shape, command reused** | **1,224 B** | **-37%** |
| ADO.NET hand-tuned | 1,094 B | -44% |
| no ADO.NET at all | 1,001 B | -49% |

Three things fall out, and the second is the important one:

- **the design reaches the target.** 1,224 B lands within ~10% of hand-tuned, so the three-layer
  shape does not cost anything meaningful over hand-written code. The thought process holds;
- **command handling is most of the gap, and it splits in two.** Measured by varying only the
  command policy on otherwise identical code:

  | single row | allocated | delta |
  | --- | ---: | ---: |
  | fresh command per call | 1,832 B | |
  | reused command, **not** prepared | 1,450 B | **-382** (object reuse) |
  | reused command, prepared | 1,208 B | **-242** (preparation) |

  Two comparable, independent wins rather than one. "Command reuse" as a single item understates it.

### `[CacheCommand]` and `[StrictTypes]`, measured

Both exist already, and the plain `[DapperAot]` row above is therefore the *default*, not the best
available. Asking for them explicitly:

| single row | allocated |
| --- | ---: |
| `[DapperAot]` | 1,881 B |
| `[DapperAot, CacheCommand]` | **1,691 B** |
| `[DapperAot, CacheCommand, StrictTypes]` | 1,749 B |

- **`[CacheCommand]` works, and captures about half of what object reuse is worth** — 190 B of the
  ~380 B available. Why it does not reach the rest is worth knowing before building anything ❓;
- **`[StrictTypes]` showed no reliable gain on top** — 1,749 against 1,691 is inside run-to-run
  jitter, and worse in that run;
- **nothing reaches preparation.** `CanPrepare` is emitted as `true`, but the ~242 B that preparing
  is worth does not appear in any configuration measured. That looks like the single largest
  unclaimed item, and it needs no new API — only for something to act on the flag ❓.

So the default-versus-configured distinction matters: an unqualified "Dapper.AOT allocates X" is
about the default, and the gap to hand-tuned is smaller than the default suggests once `[CacheCommand]`
is on.

Run-to-run jitter on allocation is around 3%, so the deltas above are real; the emitted-versus-
hand-tuned gap of ~165 B is closer to the floor and should not be over-read.

### And the timings, which reorder the whole list

Measured with a normal BenchmarkDotNet job — error bars ±5-8 µs on a ~500 µs operation, so a 30 µs
difference is several sigma rather than noise:

| single row | mean | vs Dapper | allocated |
| --- | ---: | ---: | ---: |
| Dapper | 515.9 µs | 1.00 | 1,942 B |
| `[DapperAot]` | 509.6 µs | 0.99 | 2,008 B |
| `[DapperAot, CacheCommand]` | 513.4 µs | 1.00 | 1,796 B |
| `[DapperAot, CacheCommand, StrictTypes]` | 511.7 µs | 0.99 | 1,796 B |
| ADO.NET hand-tuned | 485.9 µs | 0.94 | 1,105 B |
| **emitted, reused + prepared** | **479.6 µs** | **0.93** | 1,241 B |
| emitted, reused, **not** prepared | 512.2 µs | 0.99 | 1,486 B |
| emitted, fresh command | 506.9 µs | 0.98 | 1,869 B |
| no ADO.NET at all | 479.6 µs | 0.93 | 912 B |

**`Prepare()` is nearly the entire timing story.** Same code, only preparation differing: 512.2 µs
against 479.6 µs, a **32.6 µs** saving that is about **90% of the 36 µs spread across every stack
measured**. Everything else — command reuse, typed parameters, typed getters, the provider switch —
shares the remainder.

The corollary matters as much:

- **command object reuse buys no time**, only bytes: 506.9 µs fresh against 512.2 µs reused is
  nothing, within noise. `[CacheCommand]` likewise, 513.4 against 509.6;
- **positional parameters buy no time either**, 486.5 against 479.6 with named marginally ahead,
  which agrees with the allocation result.

So the two axes rank the work differently, and preparation is first on both: largest single item on
allocation after object reuse (~242 B), and almost the whole of the timing spread. **Nothing
currently reaches it** — `CanPrepare` is emitted as `true` and nothing acts on the flag.

**Why preparation is worth more than it looks:** it removes per-execution parse and plan work on the
*server*, so it is not client CPU at all. That buys server capacity as well as latency, and unlike a
client-side saving it does not shrink as the network gets slower.

Throughput under saturation remains unmeasured ❓ and is still worth having, since it is the regime
where client CPU becomes the limiter — but it is no longer the only instrument available.

**Positional parameters were tested and do not pay here.** A caller writes `@id`; a generator knows
the mapping at build time and could emit the query rewritten to `$1` with unnamed parameters, so the
driver never maps names to positions. Measured: 1,283 B positional against 1,224 B named — no
benefit, marginally worse. Npgsql appears to resolve the mapping once at `Prepare` and cache it on
the command ❓, leaving nothing to save per execution. Worth noting the test was the
prepared-and-reused case, which is the *best* case for that caching; the idea could still pay on
non-prepared or fresh-command paths, which the finding above suggests is where much real code sits.

**Two decisions about it, if it is ever built.** It should land *after or as part of* the emission
work rather than being retrofitted onto the current library — the rewrite has to happen where the
SQL is emitted, and doing it twice is wasted effort. And it applies **only where the SQL is a
compile-time constant literal**: a generator cannot rewrite a string it cannot see, so anything
built, interpolated or passed in keeps the caller's syntax.

**One constraint the exercise turned up the hard way:** parameters must be declared *before*
`Prepare()`. PostgreSQL records the parameter list at parse time, so a binder that adds parameters
lazily on first execution cannot also prepare — the server answers `bind message supplies 0
parameters, but prepared statement requires 1`. This is precisely why `AddParameters` and
`UpdateParameters` are separate concerns; collapsing them into "add if absent, update otherwise"
breaks preparation.

### What it costs

- **behaviour migrates from the library to the generator.** Timeout, transaction handling, buffered
  versus unbuffered, cancellation, error wrapping, connection open/close policy — all hard-won, all
  currently in one place. Re-emitting it correctly *and* keeping it in step is the actual work here,
  and the Dapper test suite as acceptance corpus is the instrument for it. This is why the major sits
  downstream of the parity work rather than parallel to it;
- **generated code freezes at generation time**, where a library fix ships by package bump. Probably
  acceptable, but it changes how fixes reach people and should be a decision rather than a discovery;
- **code size**, much reduced by the shared-body shape above, but not zero: each distinct shape still
  carries a body per specialized provider.

By [plan.md](plan.md)'s own ordering rule — nothing that adds parse-time state lands before phase 2
completes — a change of this size is phase 3 at the earliest.

## Closed ideas

### The args object is not the prize (tested, and closed)

The tempting next step is to stop erasing the argument object: add a generic-args overload so `TArgs`
is the anonymous type rather than `object?`, hoping the object then stops escaping and gets
stack-allocated. **Three measurements say no.** Recorded so it is not re-proposed.

**1. The dominant read shape cannot reach such an overload at all.** Compiled against real-looking
call sites, with both overloads present:

| call site | explicit type arg? | binds to |
| --- | :-: | --- |
| `cnn.Query<Customer>(sql, new { id })` | yes | **the existing `Query<T>(string, object?)`** |
| `cnn.Execute(sql, new { id })` | no | the generic `Execute<TArgs>` |
| `cnn.Query(sql, new { id })` (dynamic) | no | the generic `Query<TArgs>` |
| `cnn.ExecuteScalar(sql, new { id })` | no | the generic `ExecuteScalar<TArgs>` |
| `cnn.Execute(sql, null)` | no | the existing `object?` overload |
| `cnn.Execute(sql, objectTypedLocal)` | no | the existing `object?` overload |

**The line is whether the call site states a type argument**, and the two sides fail differently:

- **explicit type argument** — `Query<Customer>(...)` supplies one, so a two-parameter
  `Query<TResult, TArgs>` is not a candidate at all: **explicit type arguments must supply every type
  parameter, and C# has no partial inference**. Nor can the call be written explicitly, because the
  anonymous type has no name to give. So the shape that dominates Dapper *reads* is unreachable by
  construction rather than by oversight, and `[OverloadResolutionPriority]` is **moot** here — there
  is nothing to prioritise;
- **no explicit type argument** — the generic overload binds, and does so **without**
  `[OverloadResolutionPriority]`, because an identity conversion already beats conversion-to-`object`.
  Verified for `Execute`, dynamic-returning `Query`, and `ExecuteScalar`.

So a generic-args overload is available for exactly the non-generic-result methods — most of the
write path, plus dynamic reads — and for nothing else. `null` and `object`-typed locals keep today's
overload either way, which is the behaviour one wants.

One risk if such an overload is ever added for other reasons: `DynamicParameters`-shaped arguments
would start binding to it, and Dapper handles those specially today, so it would need an explicit
carve-out ❓.

**2. The object is small.** Measured: `new { id = 42 }` is **24 B**, `new { id, name }` is 32 B, an
ordinary named args class is 24 B. Against the ~890 B that separates Dapper.AOT today from
hand-tuned ADO.NET, the argument object is **under 3% of the gap**.

**3. Stack allocation does not happen anyway.** Probed on this runtime with an anonymous object that
is created, has one field read, and never crosses a call boundary — as non-escaping as the shape
gets — against the same object passed as `object`:

```
inlined, never crosses a boundary       24.0 B per iteration
passed as object (today's shape)        24.0 B per iteration
```

No difference. The hoped-for saving is not there to collect ❓ (one runtime, one shape — but the
direction is clear enough to stop).

**So the ~890 B is somewhere else**, and that is where the work belongs: command, parameter
collection and parameter objects per execution, the reader, the async state machines, and the boxing
of parameter *values* — which is items 1, 2 and 3 of the technique list, not the argument object.
The step-0 anomaly points the same way: no single-row win over vanilla is what one would expect if
the command is not actually being reused.

## Non-goals

- **The connection model.** Generated code sits on ADO.NET, so pooling, multiplexing and socket count
  are out of reach here regardless of how the emission is specialized. Not a gap in this plan — a
  different plan entirely.
- **Provider-specific *semantics*.** Specialization must not change which SQL is sent, which
  parameters are bound, or what values come back. If a specialized path would differ observably from
  the generic one, it is a bug in the specialization.
- **Anything that regresses the agnostic path**, which remains what most call sites get.

## Sequencing

Ordered so that each step is measurable on its own, and so the provider-agnostic wins land before the
provider-specific machinery exists.

**Step 0 — done.** The anomaly is explained: the default configuration neither re-uses nor prepares
commands. `[CacheCommand]` exists and recovers ~190 B of the ~380 B object re-use is worth; nothing
reaches preparation. So the baseline for every comparison is `[DapperAot, CacheCommand]`, and the
remaining headroom is smaller than the default suggested but still large.

**Step 1 — preparation.** On its own, ahead of everything else, because it is ~90% of the timing
spread and ~242 B, and because nothing acts on `CanPrepare` today. It also needs no provider
knowledge and no new API. The one design constraint is that parameters must be declared *before*
`Prepare()`, so whatever drives it has to run after `AddParameters` and before the first execution.
*Exit: a prepared path exists and the ~33 µs shows up on the harness.*

**Step 1b — the remaining agnostic techniques.** Items 5, 6, 7: typed getters, `SequentialAccess` +
arity, `IsDBNull` before a typed get. Plus whatever `[CacheCommand]` is leaving on the table.
*Exit: allocation moves; timing is not expected to, and that is the point of measuring both.*

**Step 2 — provider detection.** Static-type-at-call-site detection with silent fallback, and the
per-provider table above filled in by symbol resolution rather than memory.
*Exit: a call site on `NpgsqlConnection` is distinguishable from one on `DbConnection` in the
generated output, with no behavioural difference.*

**Step 3 — specialized parameters.** Items 3 and 4. On the evidence above this is **Npgsql-only**
for now, which makes it a smaller and better-bounded piece of work than it first looks — and a good
one to do first precisely because it is bounded.
*Exit: no boxing of value-type parameters on an Npgsql path; acceptance corpus green.*

**Step 3b — the major, if it is wanted.** Emitting end-to-end bodies is not required by steps 1-3;
they can land on the current factory shape. It is what makes them *cheap to write* and makes step 2's
detection switch an `if` rather than a factory hierarchy, so the honest question is whether to do
steps 1-3 twice or once. Doing the major first costs more up front and less overall ❓.
*Exit: the acceptance corpus is green through emitted bodies, with the runtime library reduced to
helpers.*

**Step 4 — re-measure, and decide whether more is worth it.** With steps 1–3 landed, the remaining
distance to "no ADO.NET at all" was 9% on the single-row path in the measurement above. If that holds
after the work, it is a reasonable place to stop.

## How to measure

Same harness shape throughout, so results stay comparable:

- **both axes, always.** They rank the work differently — preparation dominates timing while
  command re-use and specialized parameters are allocation-only — so measuring one and inferring the
  other is how a plan gets mis-ordered. This note did exactly that once;
- **a normal BenchmarkDotNet job, never `--job short`.** Short is fine for allocation, which is
  counted rather than timed, and produces error bars wider than the entire effect for timing. That
  mistake is what produced the retracted "the timings say nothing" claim;
- **the same two workloads** — a single-row primary-key lookup and a 100-row scan — so a change can
  be attributed to per-execution or per-row cost;
- **`[DapperAot, CacheCommand]` as the baseline**, not the bare default, or the headroom is
  overstated;
- **the ceiling stacks kept in the table** — hand-tuned ADO.NET and the no-ADO.NET prototype — so it
  stays obvious when a step has captured most of what is available;
- **throughput under saturation** is still worth adding ❓, since it is the regime where client CPU
  becomes the limiter. Client and server sharing a machine compete for CPU there, so pin them apart
  (`--cpuset-cpus`) before quoting anything from it.
