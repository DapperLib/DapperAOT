using Dapper.CodeAnalysis.Extensions;
using Dapper.Internal;
using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis.Writers
{
    internal struct PreGeneratedCodeWriter
    {
        readonly bool _hasInterceptsLocationAttribute;
        readonly CodeWriter _codeWriter;

        public PreGeneratedCodeWriter(
            CodeWriter codeWriter,
            Compilation compilation)
            : this(codeWriter, HasInterceptsLocationAttribute(compilation))
        { }

        public PreGeneratedCodeWriter(
            CodeWriter codeWriter,
            bool hasInterceptsLocationAttribute)
        {
            _codeWriter = codeWriter;
            _hasInterceptsLocationAttribute = hasInterceptsLocationAttribute;
        }

        /// <summary>Is <c>InterceptsLocationAttribute</c> already available to the consumer's compilation?</summary>
        internal static bool HasInterceptsLocationAttribute(Compilation compilation)
        {
            var attrib = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InterceptsLocationAttribute");
            return IsAvailable(attrib, compilation);

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
            if (!_hasInterceptsLocationAttribute)
            {
                _codeWriter.NewLine().Append(Resources.ReadString("Dapper.InGeneration.InterceptsLocationAttribute.cs"));
            }
        }
    }
}
