# `Type` vs `<T>`: the non-generic APIs, and announcing types

## The problem

The generic APIs (`Query<Foo>`) tell the generator the row type at the call site; those are
supported. The `Type`-based APIs do not:

| API | shape |
| --- | --- |
| `Query(Type type, string sql, ...)` + `First/Single[OrDefault]` + async | row type is a runtime argument |
| `Query(sql, Type[] types, Func<object[], TReturn> map, string splitOn, ...)` | arbitrary-arity multi-map over runtime types |
| `GetRowParser(reader, Type concreteType, ...)` | per-row polymorphism: read a discriminator column, pick a parser — the canonical use is *dynamic dispatch by design* |
| `ExecuteScalar` (untyped `object`) | mild version of the same |

Today every one of these is `NotAotSupported` and the call is left on vanilla Dapper — i.e.
broken under AOT, silently fine under JIT. This mirrors the protobuf-net AOT problem
(`Serializer.NonGeneric`, `PBN3011`): a call site nobody can resolve statically is exactly
the kind that fails only after trimming.

## The plan: announce the types

We can't resolve `typeof(x)` flowing through variables, but we don't have to: the consumer
**declares the closed set of candidate types**, and the generator emits per-type handlers
plus a `Type → handler` dispatch map. The `Type` argument then becomes a dictionary key, not
a reflection subject.

Sketch (naming tbd):

```csharp
// global: these types participate in Type-based Dapper calls anywhere in the assembly
[module: DapperTypes(typeof(Foo), typeof(Bar), typeof(Blah))]

// or local: scoped to a method/type, for call-site-adjacent declarations
[DapperTypes(typeof(Foo))]
public IEnumerable<object> GetThings(Type type) =>
    connection.Query(type, "select ...");
```

Design points to settle:

- **miss behavior**: a `Type` not in the announced set must throw a clear runtime error
  ("type X was not announced; add [DapperTypes]"), never fall back to reflection — same
  principle as protobuf-net's "no serializer" backstop: incomplete model, loud failure.
- **granularity**: module-level is the easy 90%; per-call-site scoping only matters if
  distinct call sites need distinct column-binding for the same type (probably: not v1).
- **reuse**: the per-type row-readers already exist for the generic path (`RowFactory<T>`);
  the announcement only adds the dispatch map, so the marginal cost per announced type is
  small.
- **analyzer support**: a `Type`-based call with no announcement in scope should get a
  diagnostic + code fix that scaffolds the attribute — the `PBN3010`/lightbulb pattern.
  When the argument is a `typeof(Foo)` literal, the fix (or the generator itself) can
  resolve it directly with no announcement needed; that sub-case is statically knowable
  and should just work.
- **multi-map `Type[]`**: with announced types, the object[]-map overload becomes emittable
  (the map function is user code taking `object[]`; we only need per-type readers + splitOn
  handling). It rides on the multi-map work, not on anything type-specific.
- **`GetRowParser(reader, Type)`**: the discriminator pattern is the one place the *runtime*
  choice is the whole point; announced types make it a supported dictionary lookup. This
  should be a headline scenario for the feature, with an example in docs.
- **serialization-adjacent traps**: announced types need the same closure rules as generic
  usage (constructor selection, member binding) — reuse the existing machinery, don't fork
  it.

## Strengthened APIs: generic counterparts for the raw surface

Announced types make the `Type`-based APIs *work*, but the better long-term answer for the
common case is that people never touch `Type` at all: every raw API should have a generic
counterpart, so `Type` is only for genuinely-runtime flows (discriminators, plugin-ish code).
The inventory today is lopsided:

| raw (`Type`-based) API | generic counterpart | status |
| --- | --- | --- |
| `Query(Type, ...)` etc | `Query<T>` etc | exists, supported |
| `Parse(Type)` / `Parse` | `Parse<T>` | exists |
| `GetTypeDeserializer(Type, reader, startBound, length, returnNullIfFirstMissing)` | `GetRowParser<T>(reader, startIndex, length, returnNullIfFirstMissing)` | **exists** — same knobs, better shape; AOT supports it. `GetTypeDeserializer` survives as the boxed form via announced types |
| `CreateParamInfoGenerator(Identity, ...)` → `Action<IDbCommand, object>` | *(none)* | **missing** — the write-side hole |

So the concrete "do we need a new generic API?" answer is: **yes, one — the parameter
binder.** Something like `GetParameterBinder<T>(sql?)` → `Action<IDbCommand, T>` (name tbd;
the `sql` argument exists because binding is SQL-dependent — list expansion, literals, and
member filtering all key off the command text). Notably `Identity` — the awkward part of the
raw API's signature — exists to key the *runtime cache*, which is exactly the concept AOT
deletes; the strengthened API should not carry it.

There is a second candidate answer already in the codebase: the Dapper.AOT runtime's own
`CommandFactory<T>` / `RowFactory<T>` *are* the strengthened pair, but the FAQ currently
disclaims them ("we might radically change that API at any time"). Part of unification is
deciding whether to **bless that surface as the supported, documented API** — in which case
the legacy raw APIs (`GetTypeDeserializer`, `CreateParamInfoGenerator`) become thin
announced-type shims over it, and the new generic API lands in Dapper (or Dapper.AOT) as its
public face.

## Relationship to `dynamic`

`Query` (non-generic, no `Type` argument → `dynamic` rows) is already generated and does
**not** need announcements — the row shape comes from the resultset, not from a type. Keep
these distinct in docs; people conflate "non-generic" with "dynamic".
