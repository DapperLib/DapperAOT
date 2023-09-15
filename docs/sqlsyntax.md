# SQL Syntax

There are many SQL syntax variants, all provider-specific. By default, Dapper static-analysis tools use a basic
match which can lead to false-positives of some errors (for example, mistaking local variables for parameters).

If the SQL variant is known, there are much more advanced tools available (at the time of writing, this is limited
to TSQL / SQL Server). There are multiple ways of letting the tools know more:

1. if the connection is statically recognized (`SqlConnection` etc rather than `DbConnection`), it will infer the SQL variant from that
2. if Dapper.AOT is installed and `[SqlSyntax(...)]` is in scope, it will use the variant specified
3. **not yet implemented** ~~if the `<Dapper_SqlSyntax>...</Dapper_SqlSyntax>` property is specified in the project file (in a `<PropertyGroup>`), it will use
   the variant specified; supported options: (not specified), `SqlServer`~~