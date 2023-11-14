# Dapper.AOT

Dapper.AOT is build-time tools that takes your existing Dapper code, and weaves voodoo (via "interceptors") to replace your Dapper calls with
code that bypasses all the reflection and ref-emit code with ahead-of-time (AOT) implementations, while also offering *usage guidance* on your Dapper usage.

Index:

- [Getting Started](/gettingstarted)
- [SQL Syntax](/sqlsyntax)
- [Generated Code](/generatedcode)
- [Bulk Copy](/bulkcopy)
- [Frequently Asked Questions](/faq)

Packages:

- [`Dapper.AOT`](https://www.nuget.org/packages/Dapper.AOT) - the full AOT and usage guidance suite
- [`Dapper.Advisor`](https://www.nuget.org/packages/Dapper.Advisor) - just the usage guidance parts

Problems? [Report them here](https://github.com/DapperLib/DapperAOT/issues)