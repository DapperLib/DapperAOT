# Special string-token handling

Dapper rewrites the command text and/or parameter set in several documented-but-obscure ways.
Every one of these is observable behavior that real code depends on; each needs an explicit
AOT position: **replicate**, **replicate with constraints**, or **refuse loudly** (analyzer
error, never silent divergence).

The contract here is **observable behavior only**: the SQL text and parameter set that reach
the provider. The regexes and rewrite internals cited below (from `Dapper/Dapper/SqlMapper.cs`
— `PackListParameters`, `GetInListRegex`, `GetLiteralTokens`, `SanitizeParameterValue`, the
pseudo-positional rewrite — line refs as of `72a54c4`) are *evidence* of that behavior, not
something to replicate structurally. AOT is free to implement all of this however it likes.

## 1. Parameter prefixes

Dapper matches parameters in SQL with any of the prefixes `@`, `:`, `$`, `?` (regex class
`[?@:$]`). AOT's SQL analysis is TSQL-centric; the non-`@` prefixes matter for Postgres /
Oracle / MySQL / OleDb corpora.

## 2. List expansion (`in @ids`)

When a parameter member's value is an `IEnumerable` (and not string/byte[]/special-cased),
Dapper rewrites the SQL and explodes the parameter:

- token grammar: `([?@:$]ids)(?!\w)` with an optional trailing `unknown` keyword (regex in
  `GetInListRegex`, ~`SqlMapper.cs:2150`); there is also a *positional* variant `?ids?` for
  pseudo-positional providers;
- non-empty list → `(@ids1, @ids2, ...)`, one `DbParameter` each; note the SQL text mutates
  **per list size**, which is exactly why the plan-cache-pollution features below exist;
- empty list → a always-false/no-row construct (plus provider-aware null semantics via
  `FeatureSupport`) rather than invalid `()` syntax;
- `Settings.PadListExpansions` → repeat the last value to round the count up, reducing
  distinct SQL shapes;
- `Settings.InListStringSplitCount` (SQL Server, string-typed lists ≥ N) → rewrites to
  `(select cast([value] as <type>) from string_split(@ids, ','))` with a *single* joined
  parameter (~`SqlMapper.cs:2349`);
- the `... in @ids unknown` suffix opts a single expansion out of smart handling.

**AOT today: nothing.** No trace in the generator or runtime library.

**AOT design note:** the *detection* is static (member type is enumerable, token appears in
SQL — the analyzer already sees both), but the *rewrite* is inherently per-invocation (list
size). So generated code needs a runtime helper that takes (sql, values) and produces the
rewritten text + parameters — an AOT-friendly library routine, not codegen per size. The
`string_split` path is a per-provider decision the generator can bake in when the syntax is
known.

## 3. Literal injection (`{=name}`)

`{=name}` (regex `(?<![\p{L}\p{N}_])\{=([\p{L}\p{N}_]+)\}`) is replaced with the **value of
the named member rendered as a SQL literal**, not a parameter. Rules (from
`SanitizeParameterValue` / `Format`):

- bool → `1` / `0`;
- enums → underlying numeric value;
- numerics → culture-invariant text;
- only value types that cannot carry injection are permitted — strings are **not** literal-injectable;
- `DynamicParameters.ReplaceLiterals` exposes the same machinery for custom parameter objects.

Primary use cases: constants the query optimizer should see (e.g. `where Type = {=Active}`),
and providers/positions where parameters are not allowed.

**AOT today:** the SQL analyzer recognizes the token (it substitutes `@name` before parsing,
`TSqlProcessor.cs:42`), and `Dapper.AOT/Internal/CompiledRegex.cs` carries the same regex —
but nothing in the runtime library or generated code appears to consume it. Assume ❌ until a
fixture proves otherwise. This one is a *good* AOT candidate: when the member value is known
at the call site the rendering rules are all compile-time decidable, and even when not, the
per-type formatting can be emitted statically.

## 4. Pseudo-positional parameters (`?foo?`)

For providers with positional-only parameters (OleDb / Access / some ODBC):

- `?foo?` tokens are rewritten to bare `?` and a parameter is added **per occurrence**, in
  textual order (`SqlMapper.cs:1918`);
- `Settings.UseIncrementalPseudoPositionalParameterNames` controls whether the synthesized
  parameters get distinct incremental names;
- interacts with list expansion (the `byPosition` arm of `GetInListRegex`).

**AOT today:** the analyzer recognizes the token for SQL analysis
(`CompiledRegex.PseudoPositional`); generation support unverified, assume ❌. The rewrite is
statically decidable when the SQL is a literal — the generator sees the exact token positions.

## 5. Parameter filtering / legacy tokens

For anonymous-type parameters, Dapper only binds members that are actually referenced in the
SQL (regex test per member, ~`SqlMapper.cs:2416`) — *unless* legacy tokens are present
(`Settings.SupportLegacyParameterTokens`, `CompiledRegex.LegacyParameter`). Consequence: an
anonymous object can carry extra members without breaking strict providers.

**AOT today:** DAP236 warns "Parameter '{0}' is not used, but will be included" — which reads
as a deliberate behavioral divergence (include rather than filter). Decide and document; on
strict providers (e.g. some ODBC) including an unused parameter is an error, so this may need
to be parity, not preference.

## 6. Stored-proc name detection

Dapper and AOT both infer text-vs-proc when `commandType` is unspecified
(`WhitespaceOrReserved` heuristic — shared regex, `DapperAotExtensions.cs:59`). Believed
equivalent; pin with a corpus test.

## Decision table (to fill in as designs land)

| token | AOT position | design sketch |
| --- | --- | --- |
| list expansion | replicate | runtime helper; analyzer already detects the shape |
| `string_split` optimization | replicate (per-syntax) | generator bakes in when provider known |
| pad expansions | replicate | same helper, setting/attr |
| `{=literal}` | replicate | compile-time formatting where possible |
| `?foo?` pseudo-positional | replicate | static rewrite when SQL is literal |
| param filtering | tbd | parity vs "include + warn" — currently diverges |
| `unknown` suffix | tbd | rare; refuse-loudly may be acceptable |
