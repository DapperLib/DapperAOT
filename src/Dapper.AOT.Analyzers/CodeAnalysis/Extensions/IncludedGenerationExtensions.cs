namespace Dapper.CodeAnalysis.Extensions
{
    internal static class IncludedGenerationExtensions
    {
        public static bool HasAny(this IncludedGeneration value, IncludedGeneration flag) => (value & flag) != 0;
    }
}
