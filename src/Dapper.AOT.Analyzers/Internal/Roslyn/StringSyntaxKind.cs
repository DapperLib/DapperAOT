namespace Dapper.Internal.Roslyn
{
    /// <summary>
    /// Represents a syntaxKind for the string usage
    /// </summary>
    internal enum StringSyntaxKind
    {
        NotRecognized,

        ConcatenatedString,
        InterpolatedString,

        /// <summary>
        /// Represents a syntax for `string.Format()` API
        /// </summary>
        FormatString
    }
}
