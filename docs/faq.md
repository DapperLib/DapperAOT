# Frequently Asked Questions

## How do I enable this?

[Getting Started](/gettingstarted)

## How does this work?

Tye build SDK from .NET 8 onwards includes support for "interceptors", which are a mechanism for emitting code during build that redirects method calls. In
the case of Dapper, this means we can replace the `QuerySingleOrDefault<Customer>(...)` call (above) and replace it with a generated method that
has a compatible signature and behaves as we expect, but: using code generated entirely at build time. Making existing code compatible with AOT is
one of the main goals of "interceptors", so: we're not doing anything unexpected here.

## Does it still need Dapper itself?

Your *project* still needs to reference Dapper, so we can see what your code is trying to do. In many cases, enabling Dapper.AOT will *completely replace* the Dapper
code, and you won't even need the Dapper library at runtime (although there are some scenarios in which it will remain - for example, if you're using the `QueryMultiple`
API, the returned `GridReader` is *defined* inside Dapper, so it will be retained).

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

## Any new features?

Yes! A few examples (there are more):

- tuple-types: Dapper can't make use of tuple-types with names, because the names don't exist at runtime. Dapper.AOT *can see the names* (it will offer guidance on
using this feature if it sees you using a tuple-type)
- bulk insert: Dapper.AOT includes a facility to treat sequences of objects as a data-reader, suitable for use with `SqlBulkCopy` (this is sikilar to FastMember)
- factory methods: Dapper.AOT supports static factory methods for row data in addition to constructors

## Can I see the generated code?

Yes; in Visual Studio, expand your project, Dependencies, Analyzers, Dapper.AOT.Analyzers, and look for the generated `.cs` files at the bottom. For other IDEs: refer
to their documentation around "generators".

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
