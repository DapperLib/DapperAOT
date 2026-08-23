# Type-handler registration: where we actually are, and the route forward

## The feature never landed

Dapper.AOT ships `TypeHandlerAttribute<TValue, TTypeHandler>` and an abstract `TypeHandler<T>`
(`src/Dapper.AOT/TypeHandlerT.cs`), and **nothing in the analyzer or generator reads either** -
`grep TypeHandler src/Dapper.AOT.Analyzers/` is empty. Consumers who write
`[module: TypeHandler<Foo, FooHandler>]` get a silent no-op; that is the substance of issues
#159, #165 and #173, and of the "wasted days" comments on them.

Three attempts exist, none merged:

- **PR #117** (2024-03) - allocates a handler per operation;
- **PR #162** (2025-07) - the most complete: a generator-side registry emitting one shared static
  per file, interceptor goldens included. Blocking defect on review: it *narrows* the shipped
  attribute's `AttributeUsage`, which is source-breaking. Its proposed DAP050/DAP051 ids are now
  taken by main (next free is **DAP053**);
- **PR #206** (2026-08, open) - a different tier entirely: honor *vanilla runtime* registrations by
  deferring to `SqlMapper.LookupDbType` at execution time plus a generated read-side bridge. Worth
  677 -> 705/793 on the Dapper suite, but it is a compatibility shim for runtime config, not the
  declarative model.

There is also an **in-repo `type-handler` branch** (last commit literally "incomplete", 2024-11,
off a Dec-2023 main) carrying the fullest design so far: a per-member/per-type attribute alongside
the per-value-type one; a much richer `TypeHandler<T>` with `Configure` / `SetNullValue` /
`IsDBNull` and - the valuable part - **`Tokenize(reader, columnOffset)` + `Parse(reader, ordinal,
token)`, the same token protocol `RowFactory` uses**, so a handler joins the generated read path
with no per-row type sniffing; and auto-handlers for enums (`EnumTypeHandler<T>`, `[EnumString]`).
The generator half is abandoned mid-statement and predates the phase-2 plain-data model, so it
would be rewritten; the *library* half is the asset to harvest.

## Constraints on any replacement

- **Binary compatibility is required**: the attribute and base class are in shipped packages, so
  they cannot be deleted - only obsoleted.
- **No generic attributes.** On .NET Framework, `GetCustomAttributes()` over an assembly or type
  carrying a generic attribute throws `NotSupportedException: Generic types are not valid` -
  poisoning unrelated third-party reflection, not just our own reads (`CustomAttributeData` is
  fine). Same wall protobuf-net hit recently, and the same resolution: **a `typeof`-based
  non-generic spelling**.
- **Not `[Conditional("DEBUG")]`**, unlike most Dapper.AOT attributes, if a package is to ship
  handler registrations for the types it owns - cross-assembly discovery needs the metadata to
  survive (the protobuf-net surrogate pattern).

## Route forward (agreed)

1. mark the existing `TypeHandlerAttribute<,>` (and its `TypeHandler<T>` constraint type)
   `[Obsolete(..., error: true)]` - it does nothing today, so this turns a silent no-op into a
   compile-time message naming the replacement, and keeps binary compatibility;
2. a **new non-generic, `typeof`-based registration attribute** plus a **new runtime handler API**,
   mirroring the *aim* of vanilla's `SqlMapper.AddTypeHandler` while owing nothing to its shape.
   The vanilla *call* API stays supported; it is the *config* API we are not obliged to mirror;
3. a diagnostic in DapperAOT mode when a runtime handler registration is detected with **no
   attribute-based registration for the same type** - the migration is tooling, not docs (a code
   fix, per protobuf-net's `AotMigrationAnalyzer`). Detection only sees registrations syntactically
   present in the compilation, so it is a helper, not a guarantee - which is the argument for
   keeping #206's runtime bridge available as the safety net, behind a switch that can close the
   world.
