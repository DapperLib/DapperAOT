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

This is using vanilla `Dapper`, and we want to enable `Dapper.AOT`. To do that, the first thing we need is the .NET 8 build SDK (or later). We *do not* need to target .NET 8 - we can still target
any framework (including .NET Framework, .NET Standard, .NET Core, or modern .NET), but the magic requires new build features. You can check what build SDK version you are using at the console:

``` txt
> dotnet --info
.NET SDK:
 Version:           8.0.100
 (snip lots more here)
```

If needed, [update your SDK from here](https://dotnet.microsoft.com/download).

Next, we simply add the `Dapper.AOT` nuget package - via the IDE, command-line, or by editing the project file directly:

``` txt
> dotnet add package Dapper.AOT
```

or

``` xml
<ItemGroup>
    <!-- ... etc ... -->
    <PackageReference Include="Dapper.AOT" />
</ItemGroup>
```

This installs `Dapper.AOT`. To check this, if we build, we see:

``` txt
> dotnet build
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Code\DapperAOT\test\UsageLinker\Product.cs(11,99): warning DAP005: 2 candidate Dapper methods detected, but none have Dapper.AOT enabled (https://aot.dapperlib.d
ev/rules/DAP005) [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
  UsageLinker -> C:\Code\DapperAOT\test\UsageLinker\bin\Debug\net8.0\win-x64\UsageLinker.dll
```

We don't want to surprise people by changing how their code works *just* by installing an additional package, so AOT isn't enabled by default. As [described in the link](https://aot.dapperlib.dev/rules/DAP005),
we do this by using `[DapperAot]`. Since we're happy to enable AOT everywhere in our project, we can use (in any C# file):


``` csharp
[module:DapperAot]
```

But if we prefer, we can annotate individual types or methods with `[DapperAot]` (or `[DapperAot(false)]`) to turn AOT usage on (or off) at any level.


Now if we build, we get a new error:

``` txt
> dotnet build
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Code\DapperAOT\test\UsageLinker\Dapper.AOT.Analyzers\Dapper.CodeAnalysis.DapperInterceptorGenerator\UsageLinker.generated.cs(6,10): error CS9137: The 'interceptors' experimental feature is not enabled in this namespace. Add '
<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>' to your project. [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
C:\Code\DapperAOT\test\UsageLinker\Dapper.AOT.Analyzers\Dapper.CodeAnalysis.DapperInterceptorGenerator\UsageLinker.generated.cs(20,10): error CS9137: The 'interceptors' experimental feature is not enabled in this namespace. Add
'<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>' to your project. [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
```

These messages are coming from the build SDK (not `Dapper.AOT`). The compiler folks *also* don't like surprises, so you need to *opt in* to the "interceptors" feature that `Dapper.AOT` is using. As the message says,
we do this by tweaking our project file:

``` xml
<PropertyGroup>
    <!-- ... etc ... -->
    <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Dapper.AOT</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

This grants permission for tools to generating "interceptors" in the `Dapper.AOT` namespace. If we build... silence:

``` txt
> dotnet build
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  UsageLinker -> C:\Code\DapperAOT\test\UsageLinker\bin\Debug\net8.0\win-x64\UsageLinker.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

That's.. underwhelming, but: [a lot is going on behind the scenes](/generatedcode). The fact that you didn't need to change your code is intentional. Your data-access code is now
working with build-time code generation, and should work with AOT deployment.

## SQL Analysis

To get the full benefit of the analyzer tools, make sure that it knows which SQL variant you're using. There are [multiple ways of doing this](/sqlsyntax). For example, if we
change our example to use `SqlConnection`:

``` csharp
public static Product GetProduct(SqlConnection connection, int productId) => connection.QueryFirst<Product>(
    "select * from Production.Product where ProductId=@productId", new { productId });
```

and build:

``` txt
> dotnet build
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Code\DapperAOT\test\UsageLinker\Product.cs(16,17): warning DAP219: SELECT columns should be specified explicitly (https://aot.dapperlib.dev/rules/DAP219) [C:\Code\DapperAOT\test\UsageLinker\UsageLinker.csproj]
```

It is telling us off [for using `select *`](https://aot.dapperlib.dev/rules/DAP219)! Since it knows we're using SQL Server and TSQL (for more info, see [SQL Syntax](/sqlsyntax)).
We can fix that (or suppress it if we prefer):

``` csharp
public static Product GetProduct(SqlConnection connection, int productId) => connection.QueryFirst<Product>(
    "select ProductID, Name, ProductNumber from Production.Product where ProductId=@productId", new { productId });
```

SQL analysis is available in both `Dapper.AOT` and `Dapper.Advisor`, and works for both C# and VB.

## Limitations and caveats

`Dapper.AOT` does not support all `Dapper` features; not all APIs are supported, and when an API is supported it might have limitations - for example, the *generic* APIs
like `Query<Foo>` should work, but the *non-generic* API passing `typeof(Foo)` *is not* supported. The underlying implementation is completely separate to `Dapper` (and *usually*
your code doesn't even need `Dapper` once compiled); there may be subtle differences in how some things behave.

In particular, any Dapper configuration (including `SqlMapper.Settings`, `ITypeHandler`, etc) are *not used*; in many cases
similar configuration is available via new `Dapper.AOT` markers. Please ask if you get stuck!

**PLEASE TEST YOUR CODE CAREFULLY**

If something looks wrong, missing, or would benefit from more usage guidance [let us know!](https://github.com/DapperLib/DapperAOT/issues/new/choose)