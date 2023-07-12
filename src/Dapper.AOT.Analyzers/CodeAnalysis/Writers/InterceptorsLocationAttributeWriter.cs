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
            if (attrib is null || attrib.DeclaredAccessibility != Accessibility.Public)
            {
                _codeWriter.NewLine().Append(Resources.ReadString("Dapper.InterceptsLocationAttribute.cs"));
            }
        }
    }
}
