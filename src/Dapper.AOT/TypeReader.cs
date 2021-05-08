using System;
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
        public abstract object ReadObject(IDataReader reader, ReadOnlySpan<int> tokens);
        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens);
        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract ValueTask<object> ReadObjectAsync(DbDataReader reader, ReadOnlySpan<int> tokens, CancellationToken cancellationToken);
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
        public object GetSchema(IDataReader reader)
            => reader is IDbColumnSchemaGenerator db
                ? db.GetColumnSchema()
                : reader.GetSchemaTable();

        /// <summary>
        /// Gets the opaque schema object for this reader
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public ReadOnlyCollection<DbColumn> GetSchema(IDbColumnSchemaGenerator reader)
            => reader.GetColumnSchema();

        /// <summary>
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for single-row scenarios, to avoid having to build a schema object</remarks>
        public void IdentifyFieldTokensFromData(IDataReader reader, Span<int> tokens, int offset = 0)
        {
            if (reader is DbDataReader db)
            {
                IdentifyFieldTokensFromData(db, tokens, offset);
            }
            else
            {
                IdentifyFieldTokensFromDataFallback(reader, tokens, offset);
            }
        }

        /// <summary>
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for single-row scenarios, to avoid having to build a schema object</remarks>
        public void IdentifyFieldTokensFromData(DbDataReader reader, Span<int> tokens, int offset = 0)
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
        private void IdentifyFieldTokensFromDataFallback(IDataReader reader, Span<int> tokens, int offset)
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
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(IDbColumnSchemaGenerator reader, Span<int> tokens)
            => IdentifyFieldTokensFromSchema(reader.GetColumnSchema(), 0, tokens);

        /// <summary>
        /// Inspects all columns and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(IDataReader reader, Span<int> tokens)
        {
            if (reader is IDbColumnSchemaGenerator db)
            {
                IdentifyFieldTokensFromSchema(db.GetColumnSchema(), 0, tokens);
            }
            else
            {
                IdentifyFieldTokensFromSchemaFallback(reader.GetSchemaTable(), 0, tokens);
            }
        }

        /// <summary>
        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(object schema, int offset, Span<int> tokens)
        {
            switch (schema)
            {
                case ReadOnlyCollection<DbColumn> cols:
                    IdentifyFieldTokensFromSchema(cols, offset, tokens);
                    break;
                case DataTable table:
                    IdentifyFieldTokensFromSchemaFallback(table, offset, tokens);
                    break;
                default:
                    ThrowUnexpectedSchemaType();
                    break;
            }
            static void ThrowUnexpectedSchemaType() => throw new ArgumentException("Unexpected schema-type", nameof(schema));
        }

        private void IdentifyFieldTokensFromSchemaFallback(DataTable schema, int offset, Span<int> tokens)
        {
            var dbColumns = schema.Rows; // each row in the schema table represents a column in the results
            var nameCol = schema.Columns["ColumnName"];
            var typeCol = schema.Columns["DataType"];
            var nullCol = schema.Columns["AllowDBNull"];
            for (int i = 0; i < tokens.Length; i++)
            {
                var col = dbColumns[offset++];
                string? name = col.IsNull(nameCol) ? null : (string)col[nameCol];
                if (string.IsNullOrWhiteSpace(name))
                {
                    tokens[i] = NoField;
                }
                else
                {
                    Type? type = col.IsNull(typeCol) ? null : (Type)col[typeCol];
                    bool allowNull = col.IsNull(nullCol) || (bool)col[nullCol];
                    tokens[i] = GetColumnToken(name!, type, allowNull);
                }
            }
        }

        /// <summary>
        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
        /// </summary>
        /// <remarks>This API is intended for multi-row scenarios</remarks>
        public void IdentifyFieldTokensFromSchema(ReadOnlyCollection<DbColumn> schema, int offset, Span<int> tokens)
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
