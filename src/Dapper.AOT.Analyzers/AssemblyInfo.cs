[module: System.Runtime.CompilerServices.SkipLocalsInit]

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Module)]
    sealed file class SkipLocalsInitAttribute : Attribute
    {

    }
}
#endif