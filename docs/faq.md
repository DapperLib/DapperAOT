# Frequently Asked Questions

## How do I enable this?

[Getting Started](/gettingstarted)

## How does this work?

The build SDK from .NET 8 onwards includes support for "interceptors", which are a mechanism for emitting code during build that redirects method calls. In
the case of Dapper, this means we can replace the `QuerySingleOrDefault<Customer>(...)` call (above) and replace it with a generated method that
has a compatible signature and behaves as we expect, but: using code generated entirely at build time. Making existing code compatible with AOT is
one of the main goals of "interceptors", so: we're not doing anything unexpected here.

## I set the interceptors property, but I still get CS9137

You've probably set the name the *other* SDK wants. The opt-in property was renamed as interceptors settled down:
`InterceptorsPreviewNamespaces` (.NET 8 SDK) became `InterceptorsNamespaces` (later SDKs), and neither SDK understands
the other's name - so a project that sets only one of them breaks whenever it moves between SDKs. Set both; they don't
conflict:

``` xml
<InterceptorsNamespaces>$(InterceptorsNamespaces);Dapper.AOT</InterceptorsNamespaces>
<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>
```

The CS9137 message always names the property *your* SDK wants, so when in doubt: believe the error, not the docs.

## Does it still need Dapper itself?

Your *project* still needs to reference Dapper, so we can see what your code is trying to do. In many cases, enabling Dapper.AOT will *completely replace* the Dapper
code, and you won't even need the Dapper library at runtime. It will remain wherever a call-site *isn't* intercepted, though - and any call-site we don't intercept is
left running vanilla Dapper, which means it works under JIT but is not AOT-safe. `QueryMultiple` is the current example: the `GridReader` it returns is *defined* inside
Dapper and cannot be constructed from outside it, so those calls are refused (and reported, via [DAP001](https://aot.dapperlib.dev/rules/DAP001)) rather than generated.

## Does it perform as well as Dapper?

Yes. Usually at least as well, sometimes better. In addition to general optimizations (made possible by not having to emit compatible IL, which is very hard), there are entire
new features being used in Dapper.AOT (for example, the `DbBatch` API is now supported, allowing multiple operations to be performed without paying latency per operation).

## Is every Dapper use-case supported?

Not yet - not by a long way:

- some APIs haven't even been considered yet
- some APIs only work in constrained scenarios
- some scenarios (like `private` types) can't be implemented because we have to obey rules that we could simply bypass in ref-emit IL
- at the moment, only direct inline Dapper usage is supported; we'll get around to looking at indirect usage via helper methods, but that isn't "today"

In these cases, we just leave the original Dapper code alone (and maybe offer guidance).

Two things worth knowing if you're weighing this up:

- *where* the line currently falls is tracked mechanically, not by hand:
  [`ApiSurface.expected.txt`](https://github.com/DapperLib/DapperAOT/blob/main/test/Dapper.AOT.Test/ApiSurface.expected.txt)
  classifies every public Dapper overload and is checked on every build, so it can't quietly age. The
  [parity notes](https://github.com/DapperLib/DapperAOT/blob/main/notes/parity.md) cover the behavioural side;
- "not supported" doesn't always mean "you get told". Some overloads are still dropped *silently* today, which is fine
  under JIT and a runtime failure under native AOT - closing that gap is ongoing work, and it's why
  **testing your code under an actual AOT publish matters**.

## Any new features?

Yes! A few examples (there are more):

- tuple-types: Dapper can't make use of tuple-types with names, because the names don't exist at runtime. Dapper.AOT *can see the names* (it will offer guidance on
using this feature if it sees you using a tuple-type)
- bulk insert: Dapper.AOT includes a facility to treat sequences of objects as a data-reader, suitable for use with `SqlBulkCopy` (this is similar to FastMember)
- factory methods: Dapper.AOT supports static factory methods for row data in addition to constructors

## Can I see the generated code?

Yes; in Visual Studio, expand your project, Dependencies, Analyzers, Dapper.AOT.Analyzers, and look for the generated `.cs` files at the bottom. For other IDEs: refer
to their documentation around "generators".

If you'd rather just have the files on disk - which works from any editor, and from the command line - add:

``` xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
```

and build; the generated code appears under `obj/<configuration>/<target-framework>/generated/`.

## Is this C# only? Does it work with VB? F#?

*Interceptors* are only supported on C# currently - so the AOT bits are C# only.

*Analyzers* (for usage guidance) are supported on C# and VB (if you're using VB you may prefer to use the [Dapper.Advisor](https://www.nuget.org/packages/Dapper.Advisor) package
instead of Dapper.AOT; this is the exact same analyzer package, but without the runtime library, since it will never be used).

Analyzers and interceptors are implemented via Roslyn, which supports C# and VB; no other languages are supported by Dapper.AOT or Dapper.Advisor.

## Can I use the new data API directly *instead of* using vanilla Dapper?

We can't physically stop you, but:

- the analyzers and generators won't trigger for this usage
- we might radically change that API at any time

On your own head be it.

## I'm still struggling

Maybe [log an issue here](https://github.com/DapperLib/DapperAOT/issues).
