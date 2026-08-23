namespace Dapper.Aot.Generated
{
    /// <summary>
    /// Adapts a vanilla Dapper type-handler (<c>SqlMapper.ITypeHandler</c>) to Dapper.AOT's
    /// <c>IDbValueHandler{T}</c>.
    /// </summary>
    /// <remarks>
    /// The runtime library cannot reference Dapper: a consumer may be using Dapper or
    /// Dapper.StrongName, and referencing either would load both and split the handler registry.
    /// Generated code has no such problem - it compiles against whichever one the consumer
    /// actually references - so the shim is emitted here rather than shipped.
    /// </remarks>
#if !DAPPERAOT_INTERNAL
    file
#endif
    sealed class VanillaTypeHandler<T> : global::Dapper.IDbValueHandler<T>
    {
        private readonly global::Dapper.SqlMapper.ITypeHandler _inner;
        public VanillaTypeHandler(global::Dapper.SqlMapper.ITypeHandler inner) => _inner = inner;

        public void SetValue(global::System.Data.Common.DbParameter parameter, T value)
            // vanilla's handlers special-case DBNull and can fault on a raw null (the struct cast
            // in TypeHandler<T>'s explicit interface implementation); vanilla coalesces first, so
            // we do too
            => _inner.SetValue(parameter, value is null ? (object)global::System.DBNull.Value : value);

        public void SetNullValue(global::System.Data.Common.DbParameter parameter)
            => _inner.SetValue(parameter, global::System.DBNull.Value);

        public T Parse(global::System.Data.Common.DbParameter parameter)
            => Convert(parameter.Value);

        public int Tokenize(global::System.Data.Common.DbDataReader reader, int columnOffset) => 0;

        public T Parse(global::System.Data.Common.DbDataReader reader, int ordinal, int token)
            => Convert(reader.GetValue(ordinal));

        private T Convert(object? value)
            => value is null or global::System.DBNull ? default! : (T)_inner.Parse(typeof(T), value)!;
    }
}
