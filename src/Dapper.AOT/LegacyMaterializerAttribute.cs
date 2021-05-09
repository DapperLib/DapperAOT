using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper
{
    /// <summary>
    /// Controls whether the legacy Dapper materializer be used in preference to the AOT materializer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
    [Conditional("DEBUG"), ImmutableObject(true)]
    public sealed class LegacyMaterializerAttribute : Attribute
    {
        /// <summary>
        /// Should the legacy Dapper materializer be used in preference to the AOT materializer?
        /// </summary>
        public bool UseLegacyMaterializer { get; }

        /// <summary>
        /// Creates a new <see cref="LegacyMaterializerAttribute"/> instance
        /// </summary>
        public LegacyMaterializerAttribute(bool useLegacyMaterializer)
            => UseLegacyMaterializer = useLegacyMaterializer;
    }
}
