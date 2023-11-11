using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;

namespace Dapper.Internal.Roslyn;
partial class LanguageHelper
{
    public static readonly LanguageHelper Null = new NullLanguageHelper();
    private sealed class NullLanguageHelper : LanguageHelper
    {
        internal override bool TryGetLiteralToken(SyntaxNode syntax, out SyntaxToken token)
        {
            token = default;
            return false;
        }

        internal override bool IsMemberAccess(SyntaxNode syntax) => false;

        internal override bool TryGetStringSpan(SyntaxToken syntax, string text, scoped in TSqlProcessor.Location location, out int skip, out int take)
        {
            skip = take = 0;
            return false;
        }

        internal override bool IsMethodDeclaration(SyntaxNode syntax) => false;
        internal override bool IsName(SyntaxNode syntax) => false;

        internal override string GetDisplayString(ISymbol symbol)
            => symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)!;

        internal override StringSyntaxKind? TryDetectOperationStringSyntaxKind(IOperation operation)
            => null;
    }
}
