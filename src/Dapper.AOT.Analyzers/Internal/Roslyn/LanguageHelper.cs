using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
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

    public static bool IsGlobalStatement(this SyntaxNode syntax, out SyntaxNode? entryPoint)
        => GetHelper(syntax.Language).IsGlobalStatement(syntax, out entryPoint);

    public static StringSyntaxKind? TryDetectOperationStringSyntaxKind(this IOperation operation)
        => GetHelper(operation.Syntax?.Language).TryDetectOperationStringSyntaxKind(operation);

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

    public static Location GetMemberLocation(this IInvocationOperation call)
        => GetMemberSyntax(call).GetLocation();

    public static SyntaxNode GetMemberSyntax(this IInvocationOperation call)
    {
        var syntax = call?.Syntax;
        if (syntax is null) return null!; // GIGO

        var helper = GetHelper(syntax.Language);
        foreach (var outer in syntax.ChildNodesAndTokens())
        {
            var outerNode = outer.AsNode();
            if (outerNode is not null && helper.IsMemberAccess(outerNode))
            {
                // if there is an identifier, we want the **last** one - think Foo.Bar.Blap(...)
                SyntaxNode? identifier = null;
                foreach (var inner in outerNode.ChildNodesAndTokens())
                {
                    var innerNode = inner.AsNode();
                    if (innerNode is not null && helper.IsName(innerNode))
                        identifier = innerNode;
                }
                // we'd prefer an identifier, but we'll allow the entire member-access
                return identifier ?? outerNode;
            }
        }
        return syntax;
    }

    public static string GetSignature(this IInvocationOperation call)
        => GetHelper(call?.Language).GetDisplayString(call?.TargetMethod!);

    public static string GetDisplayString(this ISymbol symbol)
        => GetHelper(symbol.Language).GetDisplayString(symbol);

}
internal abstract partial class LanguageHelper
{
    internal abstract bool IsMemberAccess(SyntaxNode syntax);
    internal abstract bool IsMethodDeclaration(SyntaxNode syntax);
    internal virtual bool IsGlobalStatement(SyntaxNode syntax, out SyntaxNode? entryPoint)
    {
        entryPoint = null;
        return false;
    }
    internal abstract bool TryGetLiteralToken(SyntaxNode syntax, out SyntaxToken token);
    internal abstract bool TryGetStringSpan(SyntaxToken token, string text, scoped in TSqlProcessor.Location location, out int skip, out int take);
    internal abstract bool IsName(SyntaxNode syntax);
    internal abstract string GetDisplayString(ISymbol method);

    internal virtual StringSyntaxKind? TryDetectOperationStringSyntaxKind(IOperation operation)
    {
        if (operation is null) return null;
        if (operation is IBinaryOperation)
        {
            return StringSyntaxKind.ConcatenatedString;
        }
        if (operation is IInterpolatedStringOperation)
        {
            return StringSyntaxKind.InterpolatedString;
        }
        if (operation is IInvocationOperation invocation)
        {
            // `string.Format()`
            if (invocation.TargetMethod is
                {
                    Name: "Format",
                    ContainingType: { SpecialType: SpecialType.System_String },
                    ContainingNamespace: { Name: "System" }
                })
            {
                return StringSyntaxKind.FormatString;
            }
        }

        return StringSyntaxKind.NotRecognized;
    }
}