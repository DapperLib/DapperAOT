using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Linq;

namespace Dapper.Internal.Roslyn;
partial class LanguageHelper
{
    public static readonly LanguageHelper CSharp = new CSharpLanguageHelper();
    private sealed class CSharpLanguageHelper : LanguageHelper
    {
        internal override bool TryGetLiteralToken(SyntaxNode syntax, out SyntaxToken token)
        {
            switch (syntax)
            {
                case LiteralExpressionSyntax direct:
                    token = direct.Token;
                    return true;
                case VariableDeclaratorSyntax decl when decl.Initializer?.Value is LiteralExpressionSyntax indirect:
                    token = indirect.Token;
                    return true;
                default:
                    token = default;
                    return false;
            }
        }

        internal override bool IsMemberAccess(SyntaxNode syntax)
            => syntax is MemberAccessExpressionSyntax;

        internal override bool IsMethodDeclaration(SyntaxNode syntax)
            => syntax.IsKind(SyntaxKind.MethodDeclaration);
        internal override bool IsGlobalStatement(SyntaxNode syntax, out SyntaxNode? entryPoint)
        {
            if (syntax.IsKind(SyntaxKind.GlobalStatement))
            {
                // compilation-unit relating to the top-level statement entry point
                entryPoint = syntax.Parent;
                return true;
            }
            return base.IsGlobalStatement(syntax, out entryPoint);
        }

        internal override bool IsName(SyntaxNode syntax)
            => syntax is SimpleNameSyntax; // NameSyntax?

        internal override string GetDisplayString(ISymbol symbol)
            => symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        // if (deconstruct && method.IsGenericMethod) method = method.ConstructedFrom ?? method;

        internal override bool TryGetStringSpan(SyntaxToken token, string text, scoped in TSqlProcessor.Location location, out int skip, out int take)
        {
            ReadOnlySpan<char> s;
            switch (token.Kind())
            {
                case SyntaxKind.StringLiteralToken when token.IsVerbatimStringLiteral():
                    s = text.AsSpan().Slice(2); // take off the @"
                    skip = 2 + SkipLines(ref s, location.Line - 1);
                    skip += SkipVerbatimStringCharacters(ref s, location.Column - 1);
                    take = SkipVerbatimStringCharacters(ref s, location.Length);
                    return true;
                case SyntaxKind.StringLiteralToken:
                    s = text.AsSpan().Slice(1); // take off the "
                    skip = 1;
                    // if line 1: can just use Column - otherwise input might have \r\n, or \xd\xa, or
                    // a range of other things; use Offset
                    skip += SkipEscapedStringCharacters(ref s, (location.Line == 1 ? location.Column : location.Offset) - 1);
                    take = SkipEscapedStringCharacters(ref s, location.Length);
                    return true;
                case SyntaxKind.SingleLineRawStringLiteralToken when location.Line == 1:
                    // no escape sequences; just need to skip the preamble """ etc
                    skip = CountQuotes(text) + location.Column - 1;
                    take = location.Length;
                    return true;
                case SyntaxKind.MultiLineRawStringLiteralToken:
                    var quotes = CountQuotes(text);
                    s = text.AsSpan();
                    var lastLineStart = s.LastIndexOfAny('\r', '\n');
                    if (lastLineStart < 0) break; // ????
                    var indent = s.Length - quotes - lastLineStart - 1; // last line defines the indent

                    skip = SkipLines(ref s, location.Line) // note we're also skipping the first one here
                        + indent + location.Column - 1;
                    take = location.Length;
                    return true;
            }
            skip = take = 0;
            return false;
        }

        internal override StringSyntaxKind? TryDetectOperationStringSyntaxKind(IOperation operation)
        {
            if (operation is not ILocalReferenceOperation localOperation)
            {
                return base.TryDetectOperationStringSyntaxKind(operation);
            }

            var referenceSyntax = localOperation.Local?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (referenceSyntax is VariableDeclaratorSyntax variableDeclaratorSyntax)
            {
                var initializer = variableDeclaratorSyntax.Initializer?.Value;
                if (initializer is null)
                {
                    return StringSyntaxKind.NotRecognized;
                }
                if (initializer is InterpolatedStringExpressionSyntax)
                {
                    return StringSyntaxKind.InterpolatedString;
                }
                if (initializer is BinaryExpressionSyntax)
                {
                    return StringSyntaxKind.ConcatenatedString;
                }
            }

            return StringSyntaxKind.NotRecognized;
        }
    }

    static int CountQuotes(string text)
    {
        int count = 0;
        foreach (char c in text)
        {
            if (c == '"') count++;
            else break;
        }
        return count;
    }

    static int SkipVerbatimStringCharacters(ref ReadOnlySpan<char> text, int characters)
    {
        int i;
        for (i = 0; i < text.Length && characters > 0; i++)
        {
            if (text[i] == '"' && text.Length > i + 1 && text[i + 1] == '"')
            {
                i++; // take the extra "
            }
            characters--;
        }
        text = text.Slice(i);
        return i;
    }

    static int SkipEscapedStringCharacters(ref ReadOnlySpan<char> text, int characters)
    {
        int i;
        for (i = 0; i < text.Length && characters > 0; i++)
        {
            if (text[i] == '\\' && text.Length > i + 1)
            {
                switch (text[i + 1])
                {
                    case 'u': // \uHHHH
                        i += 5;
                        break;
                    case 'U' when text.Length > i + 9: // \U00HHHHHH
                        if (text[i + 4] != '0' || text[i + 5] != 0)
                        {   // if not \U0000, then: 2-char UTF32
                            characters--;
                        }
                        i += 9;
                        break;

                    // /x is complicated - variable length
                    case 'x' when text.Length > i + 5 && IsHex(text[i + 2]) && IsHex(text[i + 3]) && IsHex(text[i + 4]) && IsHex(text[i + 5]):
                        i += 6;
                        break;
                    case 'x' when text.Length > i + 4 && IsHex(text[i + 2]) && IsHex(text[i + 3]) && IsHex(text[i + 4]):
                        i += 5;
                        break;
                    case 'x' when text.Length > i + 3 && IsHex(text[i + 2]) && IsHex(text[i + 3]):
                        i += 4;
                        break;
                    case 'x' when text.Length > i + 2 && IsHex(text[i + 2]):
                        i += 3;
                        break;
                    default:
                        i++; // \b, \r, \t, \0, etc
                        break;
                }
            }
            characters--;
        }
        if (i >= text.Length)
        {   // take everything
            text = default;
            return text.Length;
        }
        else
        {
            text = text.Slice(i);
            return i;
        }

        static bool IsHex(char c) => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
    }

    static int SkipLines(ref ReadOnlySpan<char> text, int lines)
    {
        if (lines == 0) return 0;

        var original = text;
        int skipped = 0;
        while (lines > 0 && !text.IsEmpty)
        {
            lines--;
            var i = text.IndexOfAny('\r', '\n');
            if (i < 0)
            {
                text = default;
                return original.Length; // ran out of characters
            }
            if (text[i] == '\r' && text.Length > i + 1 && text[i + 1] == '\n')
            {
                skipped += i + 2;
                text = text.Slice(i + 2);
            }
            else
            {
                skipped += i + 1;
                text = text.Slice(i + 1);
            }
        }
        return skipped;
    }

}
