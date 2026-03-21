namespace System.Diagnostics.CodeAnalysis
{
    internal sealed class NotNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            _ = returnValue;
            _ = members;
        }
    }
}
