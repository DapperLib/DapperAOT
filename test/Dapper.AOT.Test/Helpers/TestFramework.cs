using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.AOT.Test.Helpers
{
    internal static class TestFramework
    {
        public static readonly ISet<string> NetVersions 
            = ((Net[])Enum.GetValues(typeof(Net)))
            .Select(static x => x.ToString())
            .ToHashSet();

            public static Net DetermineNetVersion()
        {
#if NET6_0_OR_GREATER
            return Net.Net6;
#endif
            return Net.Net48;
        }

        public enum Net
        {
            Net48,
            Net6
        }
    }
}
