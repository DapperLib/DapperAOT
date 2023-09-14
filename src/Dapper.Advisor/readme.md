# Dapper.Advisor

For the most up-to-date contents of this file, see [GitHub](https://github.com/DapperLib/DapperAOT/blob/main/src/Dapper.Advisor/readme.md).

This tool contains the analyzers that offer guidance on Dapper usage that are included as part of [Dapper.AOT](https://www.nuget.org/packages/Dapper.AOT).
[Dapper.Advisor](https://www.nuget.org/packages/Dapper.Advisor) makes those same tips available for vanilla Dapper use-cases. We may choose to bundle
this inside Dapper itself later, but for now it is a standalone tool.

It works with both [Dapper](https://www.nuget.org/packages/Dapper) and [Dapper.StrongName](https://www.nuget.org/packages/Dapper.StrongName).

All feedback / questions / etc - see [DapperAOT](https://github.com/DapperLib/DapperAOT/).

Notes:

- the advanced SQL analysis tools are limited to SQL Server (TSQL) currently, identified via `SqlConnection` as the connection type
