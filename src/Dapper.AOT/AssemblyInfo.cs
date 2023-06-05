using System.Diagnostics;

[module: System.Runtime.CompilerServices.SkipLocalsInit]

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [Conditional("DEBUG")] // not needed post-build, so: evaporate
    [AttributeUsage(AttributeTargets.Module)]
    sealed file class SkipLocalsInitAttribute : Attribute
    {
    }
}
#endif