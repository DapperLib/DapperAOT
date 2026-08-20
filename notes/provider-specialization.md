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

| stack | single row | ratio | 100 rows | ratio |
| --- | ---: | ---: | ---: | ---: |
| vanilla Dapper | 1,888 B | 1.00 | 17,995 B | 1.00 |
| **Dapper.AOT, today** | **1,927 B** | **1.02** | **14,143 B** | **0.79** |
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

**Why no timings.** Over loopback a round trip is ~475 µs on this setup, and a hundred extra rows
cost 30 µs — so per-operation client cost sits inside a shadow a hundred times its size and the
timing column says nothing about any of these stacks. Allocation is latency-independent and is the
honest axis for this comparison. Throughput under saturation is the other honest axis and is
**unmeasured** ❓.

**One result wants an owner's eye, not a conclusion**: Dapper.AOT shows no single-row allocation win
over vanilla (1,927 vs 1,888). Either the command cache does not engage for this shape, or the
benchmark's usage is unrepresentative. Worth resolving before sizing any of the work below, since it
may already be a bug rather than a missing feature.

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

## How the emission should be structured

A separate question from *what* to emit, and it has a bearing on how reachable the numbers above
actually are.

### The case for full explicit emission

Today the generator emits a `CommandFactory` subclass and a `RowFactory`, and hands them to runtime
plumbing that owns the command lifecycle and the reader loop. An alternative for a major is to emit
the **whole operation explicitly** — create or reuse the command, set parameters, execute, loop,
materialise, dispose — as generated code, with the runtime library reduced to helpers.

Two arguments for it, and neither is "the code is more obvious", though it is:

- **it removes a type-erasure tax the current design imposes.** An anonymous type cannot be named as
  a type argument on a runtime type, so the factory for `new { id = 42 }` degrades to
  `CommandFactory<object?>` and casts back:

  ```csharp
  private sealed class CommandFactory0 : CommandFactory<object?> // <anonymous type: int id>
      var typed = Cast(args, static () => new { id = default(int) });
  ```

  Generated inline code has the anonymous type *in scope*, so the erasure costs a `castclass` rather
  than an allocation. **Note that is all it costs** — see "the args object is not the prize" below,
  which tested the tempting follow-on idea and closed it;
- **provider specialization stops being a plumbing problem.** With factories it needs a
  provider-specific factory hierarchy or generic gymnastics; with explicit emission,
  `new NpgsqlParameter<int> { TypedValue = args.id }` is a line of code and the detection switch
  above is an `if`.

**And the payoff is already estimated.** The "hand-tuned ADO.NET" row in the opening table is
essentially what full explicit emission looks like — command created once and reused, prepared,
typed parameter, typed getters, no factory indirection, no erasure. So 1,927 B → 1,035 B is the
estimate for this specific proposal, not a general aspiration.

### The args object is not the prize (tested, and closed)

The tempting next step is to stop erasing the argument object: add a generic-args overload so `TArgs`
is the anonymous type rather than `object?`, hoping the object then stops escaping and gets
stack-allocated. **Three measurements say no.** Recorded so it is not re-proposed.

**1. The dominant read shape cannot reach such an overload at all.** Compiled against real-looking
call sites, with both overloads present:

| call site | binds to |
| --- | --- |
| `cnn.Query<Customer>(sql, new { id })` | **the existing `Query<T>(string, object?)`** |
| `cnn.Execute(sql, new { id })` | the generic `Execute<TArgs>` |
| `cnn.Execute(sql, null)` | the existing `object?` overload |
| `cnn.Execute(sql, objectTypedLocal)` | the existing `object?` overload |

`Query<Customer>(...)` supplies one type argument, so a two-parameter `Query<TResult, TArgs>` is not
a candidate — **explicit type arguments must supply every type parameter, and C# has no partial
inference**. Nor can the call be written explicitly, because the anonymous type has no name to give.
So the shape that dominates Dapper reads is unreachable by construction, not by oversight.

Two useful side findings if a generic-args overload is ever wanted for *other* reasons: it binds
**without** `[OverloadResolutionPriority]`, since an identity conversion already beats
conversion-to-`object`; and `DynamicParameters`-shaped arguments would start binding to it, which
Dapper handles specially today and would need an explicit carve-out ❓.

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

### The costs, which are real

- **code size.** N call sites getting N copies of a reader loop is bad, and worse for AOT binaries.
  The mitigation that keeps most of the benefit: emit **one concrete non-virtual method per (shape,
  provider)** and have call sites — and the detection switch — dispatch to it. Devirtualization and
  the erasure win survive; the duplication is paid once per shape rather than once per call site;
- **behaviour migrates from the library to the generator.** Timeout, transaction handling, buffered
  versus unbuffered, cancellation, error wrapping, connection open/close policy — all hard-won, all
  currently in one place. Re-emitting it correctly *and* keeping it in step is the actual work, and
  the Dapper test suite as acceptance corpus is the instrument for it. That is why this is downstream
  of the parity work rather than parallel to it;
- **generated code freezes at generation time**, where a library fix ships by package bump. Probably
  acceptable, but it changes how fixes reach people and should be a decision rather than a discovery.

By [plan.md](plan.md)'s own ordering rule — nothing that adds parse-time state lands before phase 2
completes — a change of this size is phase 3 at the earliest.

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

**Step 0 — resolve the anomaly.** Establish why Dapper.AOT shows no single-row allocation win over
vanilla. Until that is understood, every number below is being measured against an unknown baseline.
*Exit: the 1,927 B is attributed, and is either fixed or explained.*

**Step 1 — the agnostic techniques.** Items 1, 2, 5, 6, 7: command reuse, preparation, typed getters,
`SequentialAccess` + arity, `IsDBNull` before a typed get. No provider knowledge required.
*Exit: the single-row and 100-row allocation figures move, measured on the same harness.*

**Step 2 — provider detection.** Static-type-at-call-site detection with silent fallback, and the
per-provider table above filled in by symbol resolution rather than memory.
*Exit: a call site on `NpgsqlConnection` is distinguishable from one on `DbConnection` in the
generated output, with no behavioural difference.*

**Step 3 — specialized parameters.** Items 3 and 4. On the evidence above this is **Npgsql-only**
for now, which makes it a smaller and better-bounded piece of work than it first looks — and a good
one to do first precisely because it is bounded.
*Exit: no boxing of value-type parameters on an Npgsql path; acceptance corpus green.*

**Step 4 — re-measure, and decide whether more is worth it.** With steps 1–3 landed, the remaining
distance to "no ADO.NET at all" was 9% on the single-row path in the measurement above. If that holds
after the work, it is a reasonable place to stop.

## How to measure

Same harness shape throughout, so results stay comparable:

- **allocation per operation** is the primary axis, being latency-independent;
- **the same two workloads** — a single-row primary-key lookup and a 100-row scan — so a change can
  be attributed to per-execution or per-row cost;
- **the same four stacks**, so the ceiling stays visible and it is obvious when a step has captured
  most of what is available;
- **throughput under saturation** is worth adding ❓, since it is the regime where per-operation CPU
  stops hiding behind the round trip. Note that client and server sharing a machine compete for CPU
  at saturation, so pin them apart (`--cpuset-cpus`) before quoting anything from it.
