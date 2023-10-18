using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dapper.Internal.Roslyn
{
    internal static class SyntaxNodeExtensions
    {
        public static SyntaxKind GetKind(this SyntaxNode syntaxNode)
        {
            if (syntaxNode is null) return SyntaxKind.None;
            return syntaxNode.Kind();
        }
    }
}
