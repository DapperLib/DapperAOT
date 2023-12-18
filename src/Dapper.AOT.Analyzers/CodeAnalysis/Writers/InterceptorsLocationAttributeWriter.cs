using Dapper.Internal;
using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis.Writers
{
    internal struct InterceptorsLocationAttributeWriter
    {
        readonly CodeWriter _codeWriter;

        public InterceptorsLocationAttributeWriter(CodeWriter codeWriter)
        {
            _codeWriter = codeWriter;
        }

        /// <summary>
        /// Writes the "InterceptsLocationAttribute" to inner <see cref="CodeWriter"/>.
        /// </summary>
        /// <remarks>Does so only when "InterceptsLocationAttribute" is NOT visible by <see cref="Compilation"/>.</remarks>
        public void Write(Compilation compilation)
        {
            var attrib = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InterceptsLocationAttribute");
            if (!IsAvailable(attrib, compilation))
            {
                _codeWriter.NewLine().Append(Resources.ReadString("Dapper.InterceptsLocationAttribute.cs"));
            }

            static bool IsAvailable(INamedTypeSymbol? type, Compilation compilation)
            {
                if (type is null) return false;
                if (type.IsFileLocal) return false; // we're definitely not in that file

                switch (type.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        // fine, we'll use it
                        return true;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                        // we can use it if we're in the same project (note we won't check IVTA)
                        return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly);
                    default:
                        return false;
                }
            }
        }
    }
}
