using System;
using System.Data.Common;

namespace Dapper.Internal;

internal sealed class NullableRowFactory<T> : RowFactory<T?> where T : struct
{
    private static NullableRowFactory<T>? _default;
    internal new static NullableRowFactory<T> Default => _default ??= new();
    public override object? Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset)
    {
        tokens[0] = reader.GetFieldType(columnOffset) == typeof(T) ? 0 : 1;
        return null;
    }
    public override T? Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
        => reader.IsDBNull(columnOffset) ? null :
            tokens[0] == 0 ? GetValueExact<T>(reader, columnOffset) : GetValue<T>(reader, columnOffset);
}
