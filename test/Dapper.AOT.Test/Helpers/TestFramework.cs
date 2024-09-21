using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.AOT.Test.Helpers
{
    internal static class TestFramework
    {
        public static ISet<string> NetVersions 
            = ((NET[])Enum.GetValues(typeof(NET)))
            .Select(static x => x.ToString())
            .ToHashSet();

            public static NET DetermineNetVersion()
        {
#if NET6_0_OR_GREATER
            return NET.net6;
#endif
            return NET.net48;
        }

        public enum NET
        {
            net48,
            net6
        }
    }
}
