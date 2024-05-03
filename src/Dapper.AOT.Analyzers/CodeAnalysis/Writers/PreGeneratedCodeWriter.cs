using Dapper.CodeAnalysis.Extensions;
using Dapper.Internal;
using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis.Writers
{
    internal struct PreGeneratedCodeWriter
    {
        readonly Compilation _compilation;
        readonly CodeWriter _codeWriter;

        public PreGeneratedCodeWriter(
            CodeWriter codeWriter,
            Compilation compilation)
        {
            _codeWriter = codeWriter;
            _compilation = compilation;
        }

        public void Write(IncludedGeneration includedGenerations)
        {
            if (includedGenerations.HasAny(IncludedGeneration.InterceptsLocationAttribute))
            {
                WriteInterceptsLocationAttribute();
            }

            if (includedGenerations.HasAny(IncludedGeneration.DbStringHelpers))
            {
                _codeWriter.NewLine().Append(Resources.ReadString("Dapper.InGeneration.DapperHelpers.cs"));
            }
        }

        void WriteInterceptsLocationAttribute()
        {
            var attrib = _compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InterceptsLocationAttribute");
            if (!IsAvailable(attrib, _compilation))
            {
                _codeWriter.NewLine().Append(Resources.ReadString("Dapper.InGeneration.InterceptsLocationAttribute.cs"));
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
