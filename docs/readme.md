# Dapper.AOT

Dapper.AOT is build-time tools that takes your existing Dapper code, and weaves voodoo (via "interceptors") to replace your Dapper calls with
code that bypasses all the reflection and ref-emit code with ahead-of-time (AOT) implementations. This has lots of advantages:

- faster to start
- fully AOT compatible
- easier to see what is happening - you can view the generated code
- [easier to maintain the library](https://github.com/DapperLib/Dapper/issues/1909)

Not all Dapper methods are supported, and there may be some kinks - this is all *very early*; as such, it is "opt-in"; to enable Dapper.AOT

1. add the `Dapper.AOT` package, [see nuget.org](https://www.nuget.org/packages/Dapper.AOT#package-reference)
2. add `[DapperAot]` at methods, types, or entire modules/assemblies (`[module:DapperAot]`/`[assembly:DapperAot]`) to turn on the magic
3. rebuild

For best results, ensure that the tools [know your SQL variant](/sqlsyntax).

The static analysis tools are also available without Dapper.AOT, in the `Dapper.Advisor` package, [see nuget.org](https://www.nuget.org/packages/Dapper.Advisor#package-reference)

Problems? [Report them here](https://github.com/DapperLib/DapperAOT/issues)