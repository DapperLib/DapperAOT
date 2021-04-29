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

This works, but: there are problems:

- it needs a *lot* of reflection and ref-emit work at runtime, which can impact cold-start performance, AOT-scenarios, and memory usage
- you need to allocate objects to convey the parameters; this sounds trivial, but they add up
- we can't really do a lot to help spot problems before they hit you at runtime (well, that's not *entirely* true - we have some in-house analyzers that do a lot of this, but not all)
- it needs a *lot* of complicated cache code
- it needs to do a lot of work to handle different command types, only identifiable at runtime
- the code is *really really hard to maintain* - it is emitting raw IL, which is buggy - and it isn't even remotely practical to emit good `async` code, forcing a lot of compromises
- due to the existing code, it is *really* hard to implement command re-use, which is a huge optimization (it *could* be done, but it would need a massive refactor)
- the existing Dapper API has... kinks (we're working on that separately)

---

So... what if we could change all that?

C# 9 offers new `partial` method support for use with Roslyn generators, allowing us to take a completely different approach to ADO.NET; consider:

``` c#
[Command("select * from Customers where Id = @id")]
public static partial Customer GetCustomer(DbConnection connection, int id);

int id = ...
var customer = GetCustomer(connection, id);
```

Here:

- everything gets generated at build; no runtime reflection/ref-emit code (eventually; incomplete)
- our parameters are expressed naturally *as parameters* - no additional object allocations
- we can even make `ref` / `out` etc work as expected (which is currently quite painful via vanilla Dapper)
- it is designed to *naturally* allow command re-use, reducing allocations considerably
- since we're already essentially writing a Roslyn analyzer, we can naturally give helpful warnings, errors, etc *during build*, so there's no surprises at runtime
  - for example, invalid SQL, mismatched parameters, possibly even query hints
- we don't have to compromise our `async` code by having it constantly jumping between generated stubs (non-`async`) and a hoisting helper
- no complicated cache lookups based on the command/types/etc
- the code can be optimized to take full advantage of the capabilies of the types it can see *during build*
- the generated code is viewable, and the code-generation code is finally maintanable
- we can think about a range of new features that previously would have been too complicated to insert into the middle of the IL
- we even have the future flexibility to consider alternative APIs, such as [Woodstar](https://github.com/dotnet/datalab)

Don't worry; we still intend to allow ad-hoc SQL usage when needed (flexible search, for example); this could just be (TBD):

``` c#
[Command]
public static partial Customer GetCustomer(DbConnection connection, [Command] string sql, int id);
```

So we're still fixing the parameters formally, but the SQL can be scenario-specific and controlled by your code.

---

Current status:

Todo:

- formal analyzer diagnostics
- materializer (currently using Dapper's materializer)
- async including cancellation
- lists/sequences (multi-row; `IEnumerable<T>`, `IAsyncEnumerable<T>`, `List<T>`, etc)
- non-queries (`void`, `Task`, `ValueTask`)
- scalars
- `out`/`ref` args (TBD: what about `async`)
- better build-time configuration
- better run-time configuration
- transactions
- ad-hoc SQL support
- ambient/implied access to connections (for example, from a `.Connection` on the current instance)
- documentation and examples
- CI, myget
- probably lots of other things

But... it works!


|                   Method |     Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------- |---------:|---------:|---------:|-------:|------:|------:|----------:|
|         DapperSystemData | 75.82 us | 0.615 us | 0.545 us | 0.3662 |     - |     - |    3.2 KB |
|      DapperMicrosoftData | 79.78 us | 1.051 us | 0.983 us | 0.3662 |     - |     - |   3.41 KB |
|    SonOfDapperSystemData | 76.57 us | 0.955 us | 0.746 us | 0.1221 |     - |     - |   1.95 KB |
| SonOfDapperMicrosoftData | 77.73 us | 0.554 us | 0.518 us | 0.1221 |     - |     - |   1.95 KB |

Dapper is already fast in the mode being tested here, so we're not looking for a speed boost; but:

- we can build on this model to help improve other scenarios where Dapper isn't quite as good
- we're reducing allocations, mostly by re-using the command instances
- and we want to look at a build-time generated materializer too, which may crank things further
