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
        public static void Register(TypeReader reader)
        {
            if (reader is not null)
            {
                s_KnownReaders[reader.Type] = reader;
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
        public object GetSchema(IDataReader reader)
            => reader is IDbColumnSchemaGenerator db
                ? db.GetColumnSchema()
                : reader.GetSchemaTable();

        /// <summary>
        /// Gets the opaque schema object for this reader
        /// </summary>
        public ReadOnlyCollection<DbColumn> GetSchema(IDbColumnSchemaGenerator reader)
            => reader.GetColumnSchema();

        /// <summary>
        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
        /// </summary>
        public void IdentifyFieldTokens(object schema, int offset, Span<int> tokens)
        {
            switch (schema)
            {
                case ReadOnlyCollection<DbColumn> cols:
                    IdentifyFieldTokens(cols, offset, tokens);
                    break;
                case DataTable table:
                    IdentifyFieldTokensFallback(table, offset, tokens);
                    break;
                default:
                    ThrowUnexpectedSchemaType();
                    break;
            }
            static void ThrowUnexpectedSchemaType() => throw new ArgumentException("Unexpected schema-type", nameof(schema));
        }

        private void IdentifyFieldTokensFallback(DataTable schema, int offset, Span<int> tokens)
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
        public void IdentifyFieldTokens(ReadOnlyCollection<DbColumn> schema, int offset, Span<int> tokens)
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
