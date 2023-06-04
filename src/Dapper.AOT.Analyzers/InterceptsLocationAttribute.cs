namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : Attribute
    {
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }
    }
}