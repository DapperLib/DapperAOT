using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;

namespace Dapper.Internal.Roslyn;

internal static class SyntaxExtensions
{
    private static LanguageHelper GetHelper(string? language)
        => language switch
        {
            LanguageNames.CSharp => LanguageHelper.CSharp,
            LanguageNames.VisualBasic => LanguageHelper.VisualBasic,
            _ => LanguageHelper.Null,
        };

    public static bool TryGetLiteralToken(this SyntaxNode? syntax, out SyntaxToken token)
        => GetHelper(syntax?.Language).TryGetLiteralToken(syntax!, out token);

    public static bool IsMemberAccess(this SyntaxNode syntax)
        => GetHelper(syntax.Language).IsMemberAccess(syntax);

    public static bool IsMethodDeclaration(this SyntaxNode syntax)
        => GetHelper(syntax.Language).IsMethodDeclaration(syntax);

    public static Location ComputeLocation(this SyntaxToken token, scoped in TSqlProcessor.Location location)
    {
        var origin = token.GetLocation();
        try
        {
            if (origin.SourceTree is not null)
            {
                var text = token.Text;
                TextSpan originSpan = token.Span;
                if (GetHelper(token.Language).TryGetStringSpan(token, text, location, out var skip, out var take))
                {
                    var finalSpan = new TextSpan(originSpan.Start + skip, take);
                    if (originSpan.Contains(finalSpan)) // make sure we haven't messed up the math!
                    {
                        return Location.Create(origin.SourceTree, finalSpan);
                    }
                }
            }
        }
        catch (Exception ex)// best efforts only
        {
            Debug.WriteLine(ex.Message);
        }
        return origin;
    }
}
internal abstract partial class LanguageHelper
{
    internal abstract bool IsMemberAccess(SyntaxNode syntax);
    internal abstract bool IsMethodDeclaration(SyntaxNode syntax);
    internal abstract bool TryGetLiteralToken(SyntaxNode syntax, out SyntaxToken token);
    internal abstract bool TryGetStringSpan(SyntaxToken token, string text, scoped in TSqlProcessor.Location location, out int skip, out int take);
}