using System;

namespace Dapper.Internal;

[Flags]
internal enum OneRowFlags
{
    None = 0,
    ThrowIfNone = 1 << 0,
    ThrowIfMultiple = 1 << 1,
}
