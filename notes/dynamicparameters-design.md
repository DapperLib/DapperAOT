# Phase 3, item 1: DynamicParameters under Dapper.AOT

The largest measured gap: DAP015 ×114 call-sites, plus it underlies most of the
ParameterTests/ProcedureTests/SqlBuilderTests behavioral failures.

## The key insight: delegate to the bag itself

`DynamicParameters` *is* runtime state - no generator can know its members. But it also
already knows how to apply itself to a command: `IDynamicParameters.AddParameters(command,
identity)` is the whole vanilla protocol, including per-parameter DbType/direction/size,
templates, literal replacement, `RemoveUnused` filtering, and storing the attached
`DbParameter`s so `Get<T>` reads outputs afterwards.

So the generated command factory for a `DynamicParameters`-typed argument should not try to
understand the bag - it should **call the bag's own vanilla implementation**. That gives
byte-exact behavioral parity for the entire DynamicParameters surface in one stroke, because
it *is* the vanilla implementation:

- `Add(name, value, dbType, direction, size, precision, scale)` - full fidelity;
- output/return parameters + `Get<T>` - automatic (the bag holds the attached parameters);
- `IParameterCallbacks.OnCompleted` - generated `PostProcess` calls it;
- templates (`new DynamicParameters(template)`) - work on JIT exactly as vanilla (it is
  vanilla's code path); under **native AOT** they fail inside Dapper's own ref-emit, which
  is the same failure vanilla has there - no regression, honestly documented;
- `AddDynamicParams`, `ReplaceLiterals` - along for the ride.

## The blocker, and the Dapper-side fix **[NEEDS SIGN-OFF]**

`AddParameters(command, identity)` consumes `identity.Sql` on its first line (literal
tokens), so a null identity NREs - and `Identity`'s constructor is internal, so generated
code cannot build one. There is also no public way to *reimplement* the protocol from
outside: the per-parameter metadata lives in private `ParamInfo`.

Proposed: a small public overload on `DynamicParameters` (or a `SqlMapper` static) in
**Dapper** itself:

```csharp
public void AddParameters(IDbCommand command)
    => AddParameters(command, /* identity built from command.CommandText */);
```

i.e. self-apply against the command's own SQL - which is what `identity.Sql` is in practice.
Useful beyond AOT (extenders have wanted a way to invoke bags directly). No release needed
yet: the harness consumes Dapper by project reference, so this can sit on a Dapper branch
until the next ship.

**Consumer-version safety**: generated code calling a new Dapper API would break consumers
on older Dapper, so the generator must *probe for the overload symbol* and only take this
path when present - otherwise the call-site keeps DAP015 and stays on vanilla (the
protobuf-net probe-for-`UnsafeAccessor` pattern).

## Generator/runtime shape

- Recognition: parameter type is `DynamicParameters`, or implements
  `SqlMapper.IDynamicParameters` (the interface case needs the same overload story - start
  with the concrete type, where the new overload is definitely present).
- Emit: `CommandFactory<global::Dapper.DynamicParameters>` whose `AddParameters` calls
  `args.AddParameters(cmd.UnderlyingCommand)`; `RequirePostProcess => true` with
  `PostProcess` invoking `((SqlMapper.IParameterCallbacks)args).OnCompleted()` when the bag
  implements it (runtime test - implementers are runtime state);
- **No command caching / no prepare** for these factories: re-applying a bag to a reused
  command would double-add parameters, and prepare needs statically-known types. Refuse
  `[CacheCommand]` combination (diagnostic) rather than misbehave;
- `UpdateParameters` for batch reuse: not applicable (a bag is a single-command concept;
  multi-exec over `IEnumerable<DynamicParameters>` stays unsupported for now);
- DAP015 splits: `DynamicParameters`-typed args become supported; `object`/`dynamic`-typed
  stay DAP015 (that is announced-types territory - the runtime type is unknowable).

## Scope check against the corpus

Statically-`DynamicParameters` sites cover the bulk of the 114; the remainder are
`object`-typed pass-throughs and helper indirection, which stay refused with an honest
diagnostic. Measure the split after implementation rather than guessing it now.
