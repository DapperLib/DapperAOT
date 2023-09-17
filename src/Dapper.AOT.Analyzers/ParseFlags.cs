using System;

namespace Dapper;

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
    DynamicParameters = 1 << 6,
    SqlAdjustedForDapperSyntax = 1 << 7, // for example '{=x}' => ' @x '
}
