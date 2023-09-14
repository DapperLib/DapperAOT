# Dapper.Advisor

For the most up-to-date contents of this file, see [GitHub](https://github.com/DapperLib/DapperAOT/blob/main/src/Dapper.Advisor/readme.md).

This tool contains analyzers that offer guidance on Dapper usage; it is
included as part of Dapper.AOT, but can also offer guidance in isolation.

All feedback / questions / etc - see [DapperAOT](https://github.com/DapperLib/DapperAOT/).

Notes:

- the advanced SQL analysis tools are limited to SQL Server (TSQL) currently, identified via `SqlConnection` as the connection type
- Dapper's query-tweaking syntax (`in @ids` and `{=val}`) are not currently supported and may appear as a false-positive syntax error
