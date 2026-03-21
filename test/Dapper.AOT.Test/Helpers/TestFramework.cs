using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.AOT.Test.Helpers
{
    internal static class TestFramework
    {
        public static readonly ISet<string> NetVersions =
#if NET48
            ((Net[])Enum.GetValues(typeof(Net)))
#else
            Enum.GetValues<Net>()
#endif
            .Select(static x => x.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            public static Net DetermineNetVersion()
        {
#if NET9_0_OR_GREATER
            return Net.Net9;
#elif NET8_0_OR_GREATER
            return Net.Net8;
#elif NET6_0_OR_GREATER
            return Net.Net6;
#else
            return Net.Net48;
#endif
        }

        public enum Net
        {
            Net48,
            Net6,
            Net8,
            Net9,
        }
    }
}
