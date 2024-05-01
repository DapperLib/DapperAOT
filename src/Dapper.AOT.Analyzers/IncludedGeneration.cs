using System;

namespace Dapper
{
    [Flags]
    internal enum IncludedGeneration
    {
        None                                 = 0,
        InterceptsLocationAttribute          = 1 << 0,
        DbStringHelpers                      = 1 << 1,
    }
}
