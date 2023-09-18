using System;

namespace Dapper.SqlAnalysis;

[Flags]
internal enum ParseFlags
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
