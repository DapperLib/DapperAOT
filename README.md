Let's face it: ADO.NET is a complicated API, and writing "good" ADO.NET code by hand is time consuming and error-prone. But a lot of times you also don't want
the ceremony of an ORM like EF or LLBLGenPro - you just want to execute SQL!

For years now, Dapper helped by providing a great low-friction way of talking to arbitrary ADO.NET databases, handling command preparation, invocation, and result parsing.

For example:

``` c#
public static Customer GetCustomer(DbConnection connection, int id)
    => connection.QuerySingleOrDefault<Customer>(
        "select * from Customers where Id = @id", new { id });
    
int id = ...
var customer = GetCustomer(connection, id);
```

This works, but it needs a *lot* of reflection and ref-emit work at runtime, which can impact cold-start performance, AOT-scenarios, and memory usage.

Dapper.AOT radically changes how Dapper works, generating the necessary code *during build*; this provides a lot of advantages:

- better cold-start performance (which is especially useful in some "serverless" scenarios where processes are very short lived)
- the ability to run in full AOT environments, where runtime emit is not allowed (and even regular reflection is somewhat limited)
- the potential to *not actually end up loading Dapper* (or the reflection pieces) at runtime, if we manage to intercept all calls
- better runtime performance due to not having to constantly check metadata caches to find previously stored strategy code
- more opportunities for the tools to spot possible issues and notify you *while writing the code*, rather than throwing at runtime
- code-gen where you can *see* what code is being generated and executed, so you don't need to wonder what we're doing under the hood
- which also means it is easier for people to contribute suggestions, fixes, optimizations etc to the code-gen

- Sound good? *It does, doesn't it.*

# How do I enable this?

First, you need the *build SDK* from .NET 8 or later (you can still *target* any framework that you want - we need the build SDK, not the runtime).

Then: install the [Dapper.AOT nuget package](https://www.nuget.org/packages/Dapper.AOT) into your existing project that has Dapper code.
This contains:

- some alternative data API library code for use at runtime
- Roslyn analyzers that inspect your Dapper usage and offer usage guidance
- Roslyn generators that write additional code during build, based on your Dapper usage

Finally, add `[module:DapperAot]` to enable Dapper.AOT everywhere - or if you prefer to be more granular, you can put `[DapperAot]` on individual types or methods
(or you can turn it *off* for specific types/methods using `[DapperAot(false)]`).

If there are additional steps required: the package will tell you.

# How does this work?

Tye build SDK from .NET 8 onwards includes support for "interceptors", which are a mechanism for emitting code during build that redirects method calls. In
the case of Dapper, this means we can replace the `QuerySingleOrDefault<Customer>(...)` call (above) and replace it with a generated method that
has a compatible signature and behaves as we expect, but: using code generated entirely at build time. Making existing code compatible with AOT is
one of the main goals of "interceptors", so: we're not doing anything unexpected here.

# Does it still need Dapper itself?

Your *project* still needs to reference Dapper, so we can see what your code is trying to do. In many cases, enabling Dapper.AOT will *completely replace* the Dapper
code, and you won't even need the Dapper library at runtime (although there are some scenarios in which it will remain - for example, if you're using the `QueryMultiple`
API, the returned `GridReader` is *defined* inside Dapper, so it will be retained).

# Does it perform as well as Dapper?

Yes. Usually at least as well, sometimes better. In addition to general optimizations (made possible by not having to emit compatible IL, which is very hard), there are entire
new features being used in Dapper.AOT (for example, the `DbBatch` API is now supported, allowing multiple operations to be performed without paying latency per operation).

# Is every Dapper use-case supported?

Not yet - not by a long way:

- some APIs haven't even been considered yet
- some APIs only work in constrained scenarios
- some scenarios (like `private` types) can't be implemented because we have to obey rules that we could simply bypass in ref-emit IL
- at the moment, only direct inline Dapper usage is supported; we'll get around to looking at indirect usage via helper methods, but that isn't "today"

In these cases, we just leave the original Dapper code alone (and maybe offer guidance).

# Any new features?

Yes! A few examples (there are more):

- tuple-types: Dapper can't make use of tuple-types with names, because the names don't exist at runtime. Dapper.AOT *can see the names* (it will offer guidance on
using this feature if it sees you using a tuple-type)
- bulk insert: Dapper.AOT includes a facility to treat sequences of objects as a data-reader, suitable for use with `SqlBulkCopy` (this is sikilar to FastMember)
- factory methods: Dapper.AOT supports static factory methods for row data in addition to constructors

# Can I see the generated code?

Yes; in Visual Studio, expand your project, Dependencies, Analyzers, Dapper.AOT.Analyzers, and look for the generated `.cs` files at the bottom. For other IDEs: refer
to their documentation around "generators".

# Is this C# only? Does it work with VB? F#?

*Interceptors* are only supported on C# currently - so the AOT bits are C# only.

*Analyzers* (for usage guidance) are supported on C# and VB (if you're using VB you may prefer to use the [Dapper.Advisor](https://www.nuget.org/packages/Dapper.Advisor) package
instead of Dapper.AOT; this is the exact same analyzer package, but without the runtime library, since it will never be used).

Analyzers and interceptors are implemented via Roslyn, which supports C# and VB; no other languages are supported by Dapper.AOT or Dapper.Advisor.

# Can I use the new data API directly *instead of* using vanilla Dapper?

We can't physically stop you, but:

- the analyzers and generators won't trigger for this usage
- we might radically change that API at any time

On your own head be it.

# I'm still struggling

Maybe [log an issue here](https://github.com/DapperLib/DapperAOT/issues).
