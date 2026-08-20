namespace Dapper.Aot.Generated
{
    // installs the runtime type-handler bridge: SqlMapper.AddTypeHandler registrations reach
    // Dapper.AOT's readers through these callbacks, compiled against *this* project's Dapper
    // (which may be Dapper or Dapper.StrongName - the library cannot reference either)
    file static class TypeHandlerBridgeInitializer
    {
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Initialize() => global::Dapper.TypeHandlerBridge.Configure(
            static type => global::Dapper.SqlMapper.HasTypeHandler(type),
            static (type, value) =>
            {
#pragma warning disable CS0618 // vanilla's decision procedure: this *is* the library usage
                _ = global::Dapper.SqlMapper.LookupDbType(type, "", false, out var handler);
#pragma warning restore CS0618
                return handler is null ? value : handler.Parse(type, value);
            });
    }
}
