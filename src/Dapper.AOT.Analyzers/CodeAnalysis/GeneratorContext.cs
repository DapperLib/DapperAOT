using System.Collections.Generic;

namespace Dapper.CodeAnalysis
{
    /// <summary>
    /// Contains data about current generation run.
    /// </summary>
    internal class GeneratorContext
    {
        /// <summary>
        /// Specifies which generation types should be included in the output.
        /// </summary>
        public IncludedGeneration IncludedGenerationTypes { get; private set; }

        public GeneratorContext()
        {
            // set default included generation types here
            IncludedGenerationTypes = IncludedGeneration.InterceptsLocationAttribute;
        }

        /// <summary>
        /// The type-handler registrations actually reached by emitted code; only these get a
        /// static, so unused registrations cost nothing (and raise no unused-field warning).
        /// </summary>
        public SortedSet<int> UsedTypeHandlers { get; } = new();

        /// <summary>
        /// Note that a registration is in use, and yield the name of its static.
        /// </summary>
        public string UseTypeHandler(int index)
        {
            UsedTypeHandlers.Add(index);
            return "TypeHandler" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Adds another generation type to the list of already included types.
        /// </summary>
        /// <param name="anotherType">another generation type to include in the output</param>
        public void IncludeGenerationType(IncludedGeneration anotherType)
        {
            IncludedGenerationTypes |= anotherType;
        }
    }
}
