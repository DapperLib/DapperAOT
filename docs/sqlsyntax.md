# SQL Syntax

There are many SQL syntax variants, all provider-specific. By default, Dapper static-analysis tools use a basic
match which can lead to false-positives of some errors (for example, mistaking local variables for parameters).

If the SQL variant is known, there are much more advanced tools available (at the time of writing, this is limited
to TSQL / SQL Server). There are multiple ways of letting the tools know more (in order):

1. if the connection is statically recognized (`SqlConnection` etc rather than `DbConnection`), it will infer the SQL variant from that
2. if Dapper.AOT is installed and `[SqlSyntax(...)]` is in scope, it will use the variant specified
3. if a `dapper.sqlsyntax = ...` entry is specified in an [analyzer configuration file](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-files) (typically a [Global AnalyzerConfig](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-files#global-analyzerconfig))
   with a known value, it will be used
4. if a `<Dapper_SqlSyntax>...</Dapper_SqlSyntax>` property is specified in the project file (inside a `<PropertyGroup>`) with a known value, it will be used
5. otherwise no SQL variant is applied

For options 3 & 4, The "known values" are the names from the [`SqlSyntax`](https://github.com/DapperLib/DapperAOT/blob/main/src/Dapper.AOT/SqlSyntax.cs) enumeration, evaluated case-insensitively.

At the current time, only the `SqlServer` option provides enhanced syntax analysis.