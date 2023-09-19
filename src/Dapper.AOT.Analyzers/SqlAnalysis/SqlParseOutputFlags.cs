using System;

namespace Dapper.SqlAnalysis;

[Flags]
internal enum SqlParseOutputFlags
{
    None = 0,
    Reliable = 1 << 0,
    SyntaxError = 1 << 1,
    Return = 1 << 2,
    Query = 1 << 3,
    Queries = 1 << 4,
    MaybeQuery = 1 << 5, // think "exec": we don't know!
    SqlAdjustedForDapperSyntax = 1 << 6, // for example '{=x}' => ' @x '
    KnownParameters = 1 << 7,
}


[Flags]
internal enum SqlParseInputFlags
{
    None = 0,
    CaseSensitive = 1 << 0,
    ValidateSelectNames = 1 << 1,
    SingleRow = 1 << 2,
    AtMostOne = 1 << 3,
    ExpectQuery = 1 << 4, // active *DO* expect a query
    ExpectNoQuery = 1 << 5, // actively do *NOT* expect a query
    SingleQuery = 1 << 6, // actively expect *exactly* one query
    SingleColumn = 1 << 7, // actively expect *exactly* one column in any query
    KnownParameters = 1 << 8, // we understand the parameters
    DebugMode = 1 << 9,
}
