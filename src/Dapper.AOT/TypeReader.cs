using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    /// <summary>
    /// Represents a handler that understands how to interpret data for a given type
    /// </summary>
    public abstract class TypeReader
    {
        /// <summary>
        /// Gets a right-sized buffer for the given size, liasing with the array-pool as necessary
        /// </summary>she
        public static ArraySegment<int> RentSegment(ref int[]? buffer, int length)
        {
            if (buffer is object)
            {
                if (buffer.Length <= length)
                    return new ArraySegment<int>(buffer, 0, length);

                // otherwise, existing buffer isn't big enough; return it
                // and we'll get a bigger one in a moment
                ArrayPool<int>.Shared.Return(buffer);
            }
            buffer = ArrayPool<int>.Shared.Rent(length);
            return new ArraySegment<int>(buffer, 0, length);
        }

        /// <summary>
        /// Gets a right-sized buffer for the given size, liasing with the array-pool as necessary
        /// </summary>
        public static Span<int> RentSpan(ref int[]? buffer, int length)
        {
            if (buffer is object)
            {
                if (buffer.Length <= length)
                    return new Span<int>(buffer, 0, length);

                // otherwise, existing buffer isn't big enough; return it
                // and we'll get a bigger one in a moment
                ArrayPool<int>.Shared.Return(buffer);
            }
            buffer = ArrayPool<int>.Shared.Rent(length);
            return new Span<int>(buffer, 0, length);
        }

        /// <summary>
        /// Return a buffer to the array-pool
        /// </summary>
        public static void Return(ref int[]? buffer)
        {
            if (buffer is not null)
            {
                ArrayPool<int>.Shared.Return(buffer);
                buffer = null;
            }
        }

        private static readonly ConcurrentDictionary<Type, TypeReader> s_KnownReaders = new();

        /// <summary>
        /// Attempt to get a registered handler for the given type
        /// </summary>
        public static TypeReader? TryGetReader(Type type)
            => s_KnownReaders.TryGetValue(type, out var reader) ? reader : null;

        /// <summary>
        /// Attempt to get a registered handler for the given type
        /// </summary>
        public static TypeReader<T>? TryGetReader<T>()
            => s_KnownReaders.TryGetValue(typeof(T), out var reader) && reader is TypeReader<T> typed ? typed: null;

        /// <summary>
        /// Register a handler for the given type
        /// </summary>
        public static bool Register(TypeReader reader, bool overwrite = true)
        {
            if (reader is null) return false;

            if (overwrite)
            {
                s_KnownReaders[reader.Type] = reader;
                return true;
            }
            else
            {
                return s_KnownReaders.TryAdd(reader.Type, reader);
            }
        }

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract object ReadObject(IDataReader reader, ReadOnlySpan<int> tokens, int offset = 0);
        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens, int offset = 0);
        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract ValueTask<object> ReadObjectAsync(DbDataReader reader, ArraySegment<int> tokens, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the type associated with this handler
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// A field token that represents an unknown field
        /// </summary>
        public const int NoField = -1;

        /// <summary>
        /// Gets the opaque schema object for this reader
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public static object? GetSchema(IDataReader reader)
            => reader is IDbColumnSchemaGenerator db
                ? db.GetColumnSchema()
                : reader.GetSchemaTable();

        /// <summary>
        /// Gets the opaque schema object for this reader
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public static ReadOnlyCollection<DbColumn> GetSchema(IDbColumnSchemaGenerator reader)
            => reader.GetColumnSchema();

        /// <summary>
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for single-row scenarios, to avoid having to build a schema object</remarks>
        internal void IdentifyFieldTokensFromData(DbDataReader reader, Span<int> tokens, int offset)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                string name = reader.GetName(offset);
                if (string.IsNullOrWhiteSpace(name))
                {
                    tokens[i] = NoField;
                }
                else
                {
                    // note: we might as well say "nullable" here; we could test .IsDbNull, but that would mean
                    // we've called it *at least* once (in the "not null" case), and possibly twice (in the "null" case);
                    // we should also prefer .IsDBNullAsync in the async case, so: just assume it could be null, and
                    // everything is simpler *and* not any less efficient
                    tokens[i] = GetColumnToken(name, reader.GetFieldType(offset), isNullable: true);
                }
                offset++;
            }
        }
        internal void IdentifyFieldTokensFromDataFallback(IDataReader reader, Span<int> tokens, int offset)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                string name = reader.GetName(offset);
                if (string.IsNullOrWhiteSpace(name))
                {
                    tokens[i] = NoField;
                }
                else
                {
                    tokens[i] = GetColumnToken(name, reader.GetFieldType(offset), reader.IsDBNull(offset));
                }
                offset++;
            }
        }

        /// <summary>
        /// The recommended upper-bound on stack-allocated tokens
        /// </summary>
        public const int MaxStackTokens = 32;

        /// <summary>
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(IDbColumnSchemaGenerator reader, Span<int> tokens)
            => IdentifyFieldTokensFromSchema(reader.GetColumnSchema(), tokens);

        /// <summary>
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(IDataReader reader, Span<int> tokens)
        {
            if (reader is IDbColumnSchemaGenerator db)
            {
                IdentifyFieldTokensFromSchema(db.GetColumnSchema(), tokens);
            }
            else
            {
                IdentifyFieldTokensFromSchemaFallback(reader.GetSchemaTable(), tokens, 0);
            }
        }

        /// <summary>
        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(object? schema, Span<int> tokens, int offset = 0)
        {
            switch (schema)
            {
                case ReadOnlyCollection<DbColumn> cols:
                    IdentifyFieldTokensFromSchema(cols, tokens, offset);
                    break;
                case DataTable table:
                    IdentifyFieldTokensFromSchemaFallback(table, tokens, offset);
                    break;
                default:
                    ThrowUnexpectedSchemaType(schema);
                    break;
            }
            
        }
        private static void ThrowUnexpectedSchemaType(object? schema)
            => throw new ArgumentException($"Unexpected schema-type: '{schema?.GetType()?.Name ?? "(null)"}'", nameof(schema));
        private void IdentifyFieldTokensFromSchemaFallback(DataTable? schema, Span<int> tokens, int offset)
        {
            if (schema is null) ThrowUnexpectedSchemaType(schema);
            var dbColumns = schema!.Rows; // each row in the schema table represents a column in the results
            var nameCol = schema.Columns["ColumnName"];
            var typeCol = schema.Columns["DataType"];
            var nullCol = schema.Columns["AllowDBNull"];
            for (int i = 0; i < tokens.Length; i++)
            {
                var col = dbColumns[offset++];
                string? name = (nameCol is null || col.IsNull(nameCol)) ? null : (string)col[nameCol];
                if (string.IsNullOrWhiteSpace(name))
                {
                    tokens[i] = NoField;
                }
                else
                {
                    Type? type = (typeCol is null || col.IsNull(typeCol)) ? null : (Type)col[typeCol];
                    bool allowNull = (nullCol is null || col.IsNull(nullCol)) || (bool)col[nullCol];
                    tokens[i] = GetColumnToken(name!, type, allowNull);
                }
            }
        }

        /// <summary>
        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(ReadOnlyCollection<DbColumn> schema, Span<int> tokens, int offset = 0)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                var col = schema[offset++];
                string name = col.ColumnName;
                tokens[i] = string.IsNullOrWhiteSpace(name)
                    ? NoField : GetColumnToken(name, col.DataType, col.AllowDBNull ?? true);
            }
        }

        /// <summary>
        /// Nominate a token for the supplied column
        /// </summary>
        protected abstract int GetColumnToken(string name, Type? type, bool isNullable);
    }
}
