# Getting Started

We're going to assume that you already have existing C# or VB code that uses [`Dapper`](https://www.nuget.org/packages/Dapper/). If you're using another .NET language: sorry,
`Dapper.AOT` doesn't have anything useful for you, but I hope you still enjoy vanilla `Dapper`!

Dapper.AOT offers two new tools:

- [`Dapper.Advisor`](https://www.nuget.org/packages/Dapper.Advisor) which offers guidance on your Dapper usage (but does not change how anything works); this works with VB and C#
- [`Dapper.AOT`](https://www.nuget.org/packages/Dapper.Advisor) which includes everything from `Dapper.Advisor` but which also includes build-time code-generation
  and some runtime library code that allows your Dapper code to work in an AOT (see below) way; this works with C# only

## What is AOT?

AOT is "ahead of time". Libraries like `Dapper` use a lot of complex reflection at runtime, either directly via the reflection API, or by emitting IL at runtime based on the types that get discovered.
This is very flexible, but has disadvantages:

- the type analysis and code generation steps can be complex and can slow down cold-start usage
- to allow that work to not be constantly repeated, complex runtime caching mechanisms are involved
- some environments *do not allow* runtime code generation
- reflection code is *very hard to maintain*, limiting who can contribute to the library
- the generated code cannot be conveniently viewed
- advanced tools like linkers and trimmers work at build-time, so cannot see the code that is going to be emitted at runtime; this means that code that the library needs is removed

`Dapper.AOT` solves all of these problems as a radical overhaul to how `Dapper` works.

## Installing Dapper.AOT

Let's imagine that we have a basic project that uses `Dapper`; our csproj might contain:

``` xml
<ItemGroup>
    <!-- ... etc ... -->
    <PackageReference Include="Dapper" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
</ItemGroup>
```

(your project file might also have `Version` attributes if you're not using [Central Package Management](https://learn.microsoft.com//nuget/consume-packages/central-package-management), but it works the same)

and we might have `Dapper` code like:

``` csharp
public class Product
{
    public static Product GetProduct(SqlConnection connection, int productId) => connection.QueryFirst<Product>(
        "select * from Production.Product where ProductId=@productId", new { productId });

    public int ProductID { get; set; }
    public string Name { get; set; }
    public string ProductNumber { get; set; }
    // etc
```

This is using vanilla `Dapper`, and we want to enable `Dapper.AOT`. To do that, the first thing we need is the .NET 8 build SDK (.NET 8 RC2 or later). We *do not* need to target .NET 8 - we can still target
any framework (including .NET Framework, .NET Standard, .NET Core, or modern .NET), but the magic requires new build features. You can check what build SDK version you are using at the console:

``` txt
> dotnet --info
.NET SDK:
 Version:           8.0.100-rc2.2.23502.2
 (snip)
```

If needed, [update your SDK from here](https://dotnet.microsoft.com/download).

Next, we simply add the `Dapper.AOT` nuget package - via the IDE, command-line, or by editing the project file directly:

``` txt
> dotnet add package Dapper.AOT --prerelease
```

or

``` xml
<ItemGroup>
    <!-- ... etc ... -->
    <PackageReference Include="Dapper.AOT" />
</ItemGroup>
```

This installs `Dapper.AOT`. To check this, if we build: it starts offering us feedback:

``` txt
> dotnet build
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Code\DapperAOT\test\UsageLinker\Product.cs(16,17): warning DAP219: SELECT columns should be specified explicitly (https://aot.dapperlib.dev/rules/DAP219) [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
```

It is telling us off using `select *`! This is because it can see that we're using `SqlConnection`, so it knows we're using TSQL (for more info, see [SQL Syntax](/sqlsyntax)). We can fix that (or suppress it if we prefer):

``` csharp
public static Product GetProduct(SqlConnection connection, int productId) => connection.QueryFirst<Product>(
    "select ProductID, Name, ProductNumber from Production.Product where ProductId=@productId", new { productId });
```

The warning goes away, but: nothing AOT-related has happened yet. We don't like to surprise people, so installing `Dapper.AOT` *by itself* doesn't *change* anything - we need to *turn it on*. We do that using `[DapperAot]` at any level. If
we want AOT to apply *everywhere* in our project, we can use:

``` csharp
[module:DapperAot]
```

But if we prefer, we can annotate individual types or methods with `[DapperAot]` (or `[DapperAot(false)]`) to turn AOT usage on (or off) at any level.


Now if we build, we get a new error:

``` txt
> dotnet build
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Code\DapperAOT\test\UsageLinker\Dapper.AOT.Analyzers\Dapper.CodeAnalysis.DapperInterceptorGenerator\UsageLinker.generated.cs(6,10): error CS9137: The 'interceptors' experimental feature is not enabled in this namespace. Add '
<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>' to your project. [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
C:\Code\DapperAOT\test\UsageLinker\Dapper.AOT.Analyzers\Dapper.CodeAnalysis.DapperInterceptorGenerator\UsageLinker.generated.cs(20,10): error CS9137: The 'interceptors' experimental feature is not enabled in this namespace. Add
'<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>' to your project. [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
```

This error message is coming from the build SDK (not `Dapper.AOT`). The compiler folks *also* don't like surprises, so you need to *opt in* to "interceptors", the feature that `Dapper.AOT` is using. As the message says,
we do this by adding:

``` xml
<PropertyGroup>
    <!-- ... etc ... -->
    <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

This grants permission for tools to generating "interceptors" in the `Dapper.AOT` namespace. If we build... silence:

``` txt
> dotnet build
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  UsageLinker -> C:\Code\DapperAOT\test\UsageLinker\bin\Debug\net8.0\win-x64\UsageLinker.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

That's.. underwhelming, but: a lot is going on under the covers. The fact that you didn't need to change your code is intentional. Your data-access code is now
working build build-time support, and should work with AOT deployment.

## Limitations and caveats

`Dapper.AOT` does not support all `Dapper` features; not all APIs are supported, and when an API is supported it might have limitations - for example, the *generic* APIs
like `Query<Foo>` should work, but the *non-generic* API passing `typeof(Foo)` *is not* supported. The underlying implementation is completely separate to `Dapper` (and *usually*
your code doesn't even need `Dapper` once compiled); there may be subtle differences in how some things behave.

**PLEASE TEST YOUR CODE CAREFULLY**

If something looks wrong, missing, or would benefit from more usage guidance [let us know!](https://github.com/DapperLib/DapperAOT/issues/new/choose)