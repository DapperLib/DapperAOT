using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// Plain-data snapshot of a <see cref="Location"/>, so cached generator model values do not
/// hold Roslyn trees alive; reconstitute (for diagnostics) only at report time.
/// </summary>
/// <remarks>
/// Only value data is stored (see the model shape test); a <see cref="Location"/> pins its
/// entire syntax tree, and a cached value that holds one keeps whole compilations alive.
/// </remarks>
internal readonly struct LocationSnapshot : IEquatable<LocationSnapshot>
{
    public readonly string Path; // the source path as the tree knows it (not path-mapped)
    public readonly string MappedPath; // per GetMappedLineSpan: honors #line and path-mapping
    public readonly int SpanStart, SpanLength;
    public readonly int StartLine, StartChar, EndLine, EndChar; // zero-based, per LinePosition
    public readonly int MappedStartLine;

    public LocationSnapshot(Location location)
    {
        var span = location.GetLineSpan();
        Path = span.Path;
        SpanStart = location.SourceSpan.Start;
        SpanLength = location.SourceSpan.Length;
        StartLine = span.StartLinePosition.Line;
        StartChar = span.StartLinePosition.Character;
        EndLine = span.EndLinePosition.Line;
        EndChar = span.EndLinePosition.Character;
        var mapped = location.GetMappedLineSpan();
        MappedPath = mapped.Path;
        MappedStartLine = mapped.StartLinePosition.Line;
    }

    public bool IsDefault => Path is null;

    /// <summary>Reconstitute a location for diagnostics; only call at report time.</summary>
    public Location AsLocation() => IsDefault ? Location.None : Location.Create(Path,
        new TextSpan(SpanStart, SpanLength),
        new LinePositionSpan(new LinePosition(StartLine, StartChar), new LinePosition(EndLine, EndChar)));

    public bool Equals(LocationSnapshot other)
        => string.Equals(Path, other.Path, StringComparison.Ordinal)
        && string.Equals(MappedPath, other.MappedPath, StringComparison.Ordinal)
        && SpanStart == other.SpanStart && SpanLength == other.SpanLength
        && StartLine == other.StartLine && StartChar == other.StartChar
        && EndLine == other.EndLine && EndChar == other.EndChar
        && MappedStartLine == other.MappedStartLine;

    public override bool Equals(object? obj) => obj is LocationSnapshot other && Equals(other);
    public override int GetHashCode()
    {
        var hash = Path is null ? 0 : StringComparer.Ordinal.GetHashCode(Path);
        hash = (hash * -47) + SpanStart;
        hash = (hash * -47) + SpanLength;
        return hash;
    }
    public override string ToString() => $"{Path}({StartLine + 1},{StartChar + 1})";
}
