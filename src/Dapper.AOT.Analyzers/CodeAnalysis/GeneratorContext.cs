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
        /// Adds another generation type to the list of already included types.
        /// </summary>
        /// <param name="anotherType">another generation type to include in the output</param>
        public void IncludeGenerationType(IncludedGeneration anotherType)
        {
            IncludedGenerationTypes |= anotherType;
        }
    }
}
