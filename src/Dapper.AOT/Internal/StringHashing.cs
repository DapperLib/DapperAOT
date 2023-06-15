using System;
using System.Buffers;

namespace Dapper.Internal;

static partial class StringHashing
{
    /// <summary>
    /// Computes a reliable hash after applying normalization rules
    /// </summary>
    public static uint NormalizedHash(string? value)
    {
        uint hash = 0;
        if (!string.IsNullOrEmpty(value))
        {   // borrowed from Roslyn's switch on string implementation
            hash = 2166136261u;
            foreach (char c in value!)
            {
                if (c == '_' || char.IsWhiteSpace(c)) continue;
                hash = (char.ToLowerInvariant(c) ^ hash) * 16777619;
            }
        }
        return hash;
    }

    /// <summary>
    /// Normalize a string
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (value!.Length > 128) return NormalizeSlow(value);
        Span<char> buffer = stackalloc char[value.Length];
        return Normalize(value, buffer);

        static string NormalizeSlow(string value)
        {
            var buffer = ArrayPool<char>.Shared.Rent(value.Length);
            var result = Normalize(value, buffer);
            ArrayPool<char>.Shared.Return(buffer);
            return result;
        }

        static string Normalize(string value, Span<char> buffer)
        {
            int len = 0;
            foreach (char c in value)
            {
                if (c == '_' || char.IsWhiteSpace(c)) continue;
                buffer[len++] = char.ToLowerInvariant(c);
            }
            if (len == 0) return "";
            buffer = buffer.Slice(0, len);
            if (buffer.SequenceEqual(value.AsSpan())) return value;
#if NETCOREAPP3_1_OR_GREATER
            return new string(buffer);
#else
            unsafe
            {
                fixed (char* ptr = buffer)
                {
                    return new string(ptr, 0, len);
                }
            }
#endif

        }
    }

    /// <summary>
    /// Compares a test <paramref name="value"/> against a <paramref name="normalized"/> value.
    /// </summary>
    public static bool NormalizedEquals(string? value, string? normalized)
    {
        if (value is null | normalized is null) return ReferenceEquals(value, normalized);
        var maxLen = normalized!.Length;
        if (value!.Length < maxLen) return false;

        int index = 0;
        foreach (char c in value)
        {
            if (c == '_' || char.IsWhiteSpace(c)) continue;
            if (index == maxLen) return false;
            if (char.ToLowerInvariant(c) != normalized[index++]) return false;
        }
        return index == maxLen;
    }
}
