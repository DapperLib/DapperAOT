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

This works, but it needs a *lot* of reflection and ref-emit work at runtime, which can impact cold-start performance, AOT-scenarios, and memory usage. The
[proposed introduction of "interceptors"](https://github.com/dotnet/csharplang/issues/7009) gives us some opportunities to do some voodoo here, essentially
subverting your code, so that we find the `.QuerySingleOrDefault<Customer>(` usage *during build*, inspect it to understand your intent and your types, and
generate an *interceptor* that changes that call to instead invoke a customized path that bypasses all the reflection work, and instead gets straight to
business talking to the database. This has the advantages:

- better cold-start performance (which is especially useful in some "serverless" scenarios where processes are very short lived)
- the ability to run in full AOT environments, where runtime emit is not allowed (and even regular reflection is somewhat limited)
- the potential to *not actually end up loading Dapper* (or the reflection pieces) at runtime, if we manage to intercept all calls
- better runtime performance due to not having to constantly check metadata caches to find previously stored strategy code
- more opportunities for the tools to spot possible issues and notify you *while writing the code*, rather than throwing at runtime
- code-gen where you can *see* what code is being generated and executed, so you don't need to wonder what we're doing under the hood
- which also means it is easier for people to contribute suggestions, fixes, optimizations etc to the code-gen

And *all of this* can be achieved with *zero* code-changes by the consumer - simply by adding in the Dapper.AOT package! Sound good? *It does, doesn't it.*

DapperAOT via interceptors is currently a proof-of-concept, and only supports very limited scenarios, but: *it does actually work* (again: in limited cases).
