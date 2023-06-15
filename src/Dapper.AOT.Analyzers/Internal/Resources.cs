using System.IO;

namespace Dapper.Internal;

internal static class Resources
{
    public static string ReadString(string resourceName)
    {
        using Stream stream = typeof(Resources).Assembly.GetManifestResourceStream(resourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
