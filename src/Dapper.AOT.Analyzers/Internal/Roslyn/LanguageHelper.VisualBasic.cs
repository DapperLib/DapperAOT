using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Linq;

namespace Dapper.Internal.Roslyn;

partial class LanguageHelper
{
    public static readonly LanguageHelper VisualBasic = new VisualBasicLanguageHelper();
    private sealed class VisualBasicLanguageHelper : LanguageHelper
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
            => syntax.IsKind(SyntaxKind.DeclareSubStatement) || syntax.IsKind(SyntaxKind.DeclareFunctionStatement);
        internal override bool IsName(SyntaxNode syntax)
            => syntax is SimpleNameSyntax; // NameSyntax?

        internal override string GetDisplayString(ISymbol symbol)
            => symbol.ToDisplayString(SymbolDisplayFormat.VisualBasicShortErrorMessageFormat);

        internal override bool TryGetStringSpan(SyntaxToken token, string text, scoped in TSqlProcessor.Location location, out int skip, out int take)
        {
            ReadOnlySpan<char> s;
            switch (token.Kind())
            {
                // VB strings are multi-line verbatim literals
                case SyntaxKind.StringLiteralToken:
                    s = text.AsSpan().Slice(1); // take off the "
                    skip = 1 + SkipLines(ref s, location.Line - 1);
                    skip += SkipVerbatimStringCharacters(ref s, location.Column - 1);
                    take = SkipVerbatimStringCharacters(ref s, location.Length);
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
            var variableDeclaratorSyntax = FindVariableDeclarator();
            if (variableDeclaratorSyntax is not null)
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

            VariableDeclaratorSyntax? FindVariableDeclarator()
            {
                if (referenceSyntax is ModifiedIdentifierSyntax modifiedIdentifierSyntax)
                {
                    if (modifiedIdentifierSyntax.Parent is VariableDeclaratorSyntax)
                    {
                        return (VariableDeclaratorSyntax)modifiedIdentifierSyntax.Parent;
                    }
                }

                if (referenceSyntax is VariableDeclaratorSyntax varDeclar) return varDeclar;
                return null;
            }
        }
    }
}