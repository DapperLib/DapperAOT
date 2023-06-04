// TODO remove this
namespace System.Runtime.CompilerServices;

/// <summary>REMOVE THIS</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptsLocationAttribute : Attribute
{
    /// <summary>REMOVE THIS</summary>
    public InterceptsLocationAttribute(string x, int y, int z)
    {
        _ = x;
        _ = y;
        _ = z;
    }
}