using System;
using System.ComponentModel;

namespace Dapper;

/// <summary>
/// Bridges runtime Dapper type-handler registrations (SqlMapper.AddTypeHandler) into
/// Dapper.AOT's readers. This library deliberately does not reference Dapper (a consumer
/// may be using Dapper or Dapper.StrongName, and a hard reference would load - and split
/// the handler registry between - both); instead, generated code, which compiles against
/// the consumer's own Dapper, installs these callbacks from a module initializer.
/// </summary>
[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeHandlerBridge
{
    private static Func<Type, bool>? s_hasHandler;
    private static Func<Type, object, object?>? s_parse;

    /// <summary>
    /// Installs the lookup callbacks; intended to be called from generated code only
    /// </summary>
    public static void Configure(Func<Type, bool> hasHandler, Func<Type, object, object?> parse)
    {
        s_hasHandler = hasHandler ?? throw new ArgumentNullException(nameof(hasHandler));
        s_parse = parse ?? throw new ArgumentNullException(nameof(parse));
    }

    internal static bool Has(Type type) => s_hasHandler?.Invoke(type) ?? false;

    internal static bool TryParse(Type type, object value, out object? parsed)
    {
        if (s_hasHandler?.Invoke(type) == true)
        {
            parsed = s_parse!(type, value);
            return true;
        }
        parsed = null;
        return false;
    }
}
