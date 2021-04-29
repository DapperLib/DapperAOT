using System;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace Dapper.Internal
{
    /// <summary>
    /// This type is not intended for public consumption. Please just don't, thanks.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is not intended for public consumption. Please just don't, thanks.")]
    public static class InternalUtilities
    {
#pragma warning disable CS1591
        static readonly object[] s_BoxedInt32 = new object[] { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        static readonly object s_BoxedTrue = true, s_BoxedFalse = false;
        public static object AsValue(int value)
            => value >= -1 && value <= 10 ? s_BoxedInt32[value + 1] : value;
        public static object AsValue(int? value)
            => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;
        public static object AsValue(bool value)
            => value ? s_BoxedTrue : s_BoxedFalse;
        public static object AsValue(bool? value)
            => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;
        // ... and a few others

        public static object AsValue(object value)
            => value ?? DBNull.Value;

        public static int GetFieldNumbers(Span<int> fieldNumbers, IDataReader reader, Func<string, int> selector)
        {
            var count = reader.FieldCount;
            for (int i = 0; i < count; i++)
            {
                fieldNumbers[i] = selector(reader.GetName(i));
            }
            return count;
        }

        private static readonly int[] s_One = new int[1];

        public static void ThrowMultiple() => s_One.Single();
#pragma warning restore CS1591
    }
}
