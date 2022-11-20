//using Dapper.Internal;
//using System;
//using System.Buffers;
//using System.Collections.Concurrent;
//using System.Collections.ObjectModel;
//using System.Data;
//using System.Data.Common;
//using System.Data.SqlTypes;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Dapper
//{
//    /// <summary>
//    /// Represents a handler that understands how to interpret data for a given type
//    /// </summary>
//    public abstract class TypeReader
//    {
//        /// <summary>
//        /// Gets a right-sized buffer for the given size, liasing with the array-pool as necessary
//        /// </summary>she
//        public static ArraySegment<int> RentSegment(ref int[]? buffer, int length)
//        {
//            if (buffer is object)
//            {
//                if (buffer.Length <= length)
//                    return new ArraySegment<int>(buffer, 0, length);

//                // otherwise, existing buffer isn't big enough; return it
//                // and we'll get a bigger one in a moment
//                ArrayPool<int>.Shared.Return(buffer);
//            }
//            buffer = ArrayPool<int>.Shared.Rent(length);
//            return new ArraySegment<int>(buffer, 0, length);
//        }

//        /// <summary>
//        /// Gets a right-sized buffer for the given size, liasing with the array-pool as necessary
//        /// </summary>
//        public static Span<int> RentSpan(ref int[]? buffer, int length)
//        {
//            if (buffer is object)
//            {
//                if (buffer.Length <= length)
//                    return new Span<int>(buffer, 0, length);

//                // otherwise, existing buffer isn't big enough; return it
//                // and we'll get a bigger one in a moment
//                ArrayPool<int>.Shared.Return(buffer);
//            }
//            buffer = ArrayPool<int>.Shared.Rent(length);
//            return new Span<int>(buffer, 0, length);
//        }

//        /// <summary>
//        /// Return a buffer to the array-pool
//        /// </summary>
//        public static void Return(ref int[]? buffer)
//        {
//            if (buffer is not null)
//            {
//                ArrayPool<int>.Shared.Return(buffer);
//                buffer = null;
//            }
//        }

//        private static readonly ConcurrentDictionary<Type, TypeReader> s_KnownReaders = new();

//        /// <summary>
//        /// Attempt to get a registered handler for the given type
//        /// </summary>
//        public static TypeReader? TryGetReader(Type type)
//            => s_KnownReaders.TryGetValue(type, out var reader) ? reader : null;

//        /// <summary>
//        /// Attempt to get a registered handler for the given type
//        /// </summary>
//        public static TypeReader<T>? TryGetReader<T>()
//            => s_KnownReaders.TryGetValue(typeof(T), out var reader) && reader is TypeReader<T> typed ? typed: null;

//        /// <summary>
//        /// Register a handler for the given type
//        /// </summary>
//        public static bool Register(TypeReader reader, bool overwrite = true)
//        {
//            if (reader is null) return false;

//            if (overwrite)
//            {
//                s_KnownReaders[reader.Type] = reader;
//                return true;
//            }
//            else
//            {
//                return s_KnownReaders.TryAdd(reader.Type, reader);
//            }
//        }

//        /// <summary>
//        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
//        /// </summary>
//        public abstract object ReadObject(IDataReader reader, ReadOnlySpan<int> tokens, int offset = 0);
//        /// <summary>
//        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
//        /// </summary>
//        public abstract object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens, int offset = 0);
//        /// <summary>
//        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
//        /// </summary>
//        public abstract ValueTask<object> ReadObjectAsync(DbDataReader reader, ArraySegment<int> tokens, CancellationToken cancellationToken);
//        /// <summary>
//        /// Gets the type associated with this handler
//        /// </summary>
//        public abstract Type Type { get; }

//        /// <summary>
//        /// A field token that represents an unknown field
//        /// </summary>
//        public const int NoField = -1;

//        /// <summary>
//        /// Gets the opaque schema object for this reader
//        /// </summary>
//        /// <remarks>This API is intended for multi-row scenarios</remarks>
//        public static object? GetSchema(IDataReader reader)
//            => reader is IDbColumnSchemaGenerator db
//                ? db.GetColumnSchema()
//                : reader.GetSchemaTable();

//        /// <summary>
//        /// Gets the opaque schema object for this reader
//        /// </summary>
//        /// <remarks>This API is intended for multi-row scenarios</remarks>
//        public static ReadOnlyCollection<DbColumn> GetSchema(IDbColumnSchemaGenerator reader)
//            => reader.GetColumnSchema();

//        /// <summary>
//        /// Inspects all columns and resolves them into tokens that the handler understands
//        /// </summary>
//        /// <remarks>This API is intended for single-row scenarios, to avoid having to build a schema object</remarks>
//        internal void IdentifyFieldTokensFromData(DbDataReader reader, Span<int> tokens, int offset)
//        {
//            char[]? normalized = null;
//            for (int i = 0; i < tokens.Length; i++)
//            {
//                string name = reader.GetName(offset);
//                int token;
//                if (string.IsNullOrWhiteSpace(name))
//                {
//                    token = NoField;
//                }
//                else
//                {
//                    // note: we might as well say "nullable" here; we could test .IsDbNull, but that would mean
//                    // we've called it *at least* once (in the "not null" case), and possibly twice (in the "null" case);
//                    // we should also prefer .IsDBNullAsync in the async case, so: just assume it could be null, and
//                    // everything is simpler *and* not any less efficient
//                    var type = reader.GetFieldType(offset);
//                    token = GetColumnToken(name, type, isNullable: true);
//                    if (token < 0)
//                    {
//                        var len = Normalize(name, ref normalized);
//                        if (len != 0) token = GetColumnToken(new ReadOnlySpan<char>(normalized, 0, len), type, isNullable: true);
//                    }
//                }
//                tokens[i] = token;
//                offset++;
//            }
//            if (normalized is not null) ArrayPool<char>.Shared.Return(normalized);
//        }

//        internal static int Normalize(string name, ref char[]? output)
//        {
//            if (string.IsNullOrWhiteSpace(name)) return 0;
//            if (output is null)
//            {
//                output = ArrayPool<char>.Shared.Rent(name.Length);
//            }
//            else if (output.Length < name.Length)
//            {
//                ArrayPool<char>.Shared.Return(output);
//                output = ArrayPool<char>.Shared.Rent(name.Length);
//            }
//            int length = 0;
//            foreach (char c in name)
//            {
//                if (c == '_' || char.IsWhiteSpace(c)) continue;
//                output[length++] = char.ToLowerInvariant(c);
//            }
//            return length;
//        }

//        internal void IdentifyFieldTokensFromDataFallback(IDataReader reader, Span<int> tokens, int offset)
//        {
//            char[]? normalized = null;
//            for (int i = 0; i < tokens.Length; i++)
//            {
//                string name = reader.GetName(offset);
//                int token;
//                if (string.IsNullOrWhiteSpace(name))
//                {
//                    token = NoField;
//                }
//                else
//                {
//                    var type = reader.GetFieldType(offset);
//                    bool isNullable = reader.IsDBNull(offset);
//                    token = GetColumnToken(name, type, isNullable);
//                    if (token < 0)
//                    {
//                        var len = Normalize(name, ref normalized);
//                        if (len != 0) token = GetColumnToken(new ReadOnlySpan<char>(normalized, 0, len), type, isNullable);
//                    }
//                    tokens[i] = token;
//                }
//                tokens[i] = token;
//                offset++;
//            }
//            if (normalized is not null) ArrayPool<char>.Shared.Return(normalized);
//        }

//        /// <summary>
//        /// The recommended upper-bound on stack-allocated tokens
//        /// </summary>
//        public const int MaxStackTokens = 32;

//        /// <summary>
//        /// Inspects all columns and resolves them into tokens that the handler understands
//        /// </summary>
//        /// <remarks>This API is intended for multi-row scenarios</remarks>
//        public void IdentifyFieldTokensFromSchema(IDbColumnSchemaGenerator reader, Span<int> tokens)
//            => IdentifyFieldTokensFromSchema(reader.GetColumnSchema(), tokens);

//        /// <summary>
//        /// Inspects all columns and resolves them into tokens that the handler understands
//        /// </summary>
//        /// <remarks>This API is intended for multi-row scenarios</remarks>
//        public void IdentifyFieldTokensFromSchema(IDataReader reader, Span<int> tokens)
//        {
//            if (reader is IDbColumnSchemaGenerator db)
//            {
//                IdentifyFieldTokensFromSchema(db.GetColumnSchema(), tokens);
//            }
//            else
//            {
//                IdentifyFieldTokensFromSchemaFallback(reader.GetSchemaTable(), tokens, 0);
//            }
//        }

//        /// <summary>
//        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
//        /// </summary>
//        /// <remarks>This API is intended for multi-row scenarios</remarks>
//        public void IdentifyFieldTokensFromSchema(object? schema, Span<int> tokens, int offset = 0)
//        {
//            switch (schema)
//            {
//                case ReadOnlyCollection<DbColumn> cols:
//                    IdentifyFieldTokensFromSchema(cols, tokens, offset);
//                    break;
//                case DataTable table:
//                    IdentifyFieldTokensFromSchemaFallback(table, tokens, offset);
//                    break;
//                default:
//                    ThrowUnexpectedSchemaType(schema);
//                    break;
//            }
            
//        }
//        private static void ThrowUnexpectedSchemaType(object? schema)
//            => throw new ArgumentException($"Unexpected schema-type: '{schema?.GetType()?.Name ?? "(null)"}'", nameof(schema));
//        private void IdentifyFieldTokensFromSchemaFallback(DataTable? schema, Span<int> tokens, int offset)
//        {
//            if (schema is null) ThrowUnexpectedSchemaType(schema);
//            var dbColumns = schema!.Rows; // each row in the schema table represents a column in the results
//            var nameCol = schema.Columns["ColumnName"];
//            var typeCol = schema.Columns["DataType"];
//            var nullCol = schema.Columns["AllowDBNull"];
//            char[]? normalized = null;
//            for (int i = 0; i < tokens.Length; i++)
//            {
//                var col = dbColumns[offset++];
//                string? name = (nameCol is null || col.IsNull(nameCol)) ? null : (string)col[nameCol];
//                int token;
//                if (string.IsNullOrWhiteSpace(name))
//                {
//                    token = NoField;
//                }
//                else
//                {
//                    Type? type = (typeCol is null || col.IsNull(typeCol)) ? null : (Type)col[typeCol];
//                    bool allowNull = (nullCol is null || col.IsNull(nullCol)) || (bool)col[nullCol];
//                    token = GetColumnToken(name!, type, allowNull);
//                    if (token < 0)
//                    {
//                        var len = Normalize(name, ref normalized);
//                        if (len != 0) token = GetColumnToken(new ReadOnlySpan<char>(normalized, 0, len), type, allowNull);
//                    }
//                }
//                tokens[i] = token;
//            }
//            if (normalized is not null) ArrayPool<char>.Shared.Return(normalized);
//        }

//        /// <summary>
//        /// Inspects a range of columns (starting from <c>offset</c>) and resolves them into tokens that the handler understands
//        /// </summary>
//        /// <remarks>This API is intended for multi-row scenarios</remarks>
//        public void IdentifyFieldTokensFromSchema(ReadOnlyCollection<DbColumn> schema, Span<int> tokens, int offset = 0)
//        {
//            char[]? normalized = null;
//            for (int i = 0; i < tokens.Length; i++)
//            {
//                var col = schema[offset++];
//                string name = col.ColumnName;
//                int token;
//                if (string.IsNullOrWhiteSpace(name))
//                {
//                    token = NoField;
//                }
//                else
//                {
//                    token = GetColumnToken(name, col.DataType, col.AllowDBNull ?? true);
//                    if (token < 0)
//                    {
//                        var len = Normalize(name, ref normalized);
//                        if (len != 0) token = GetColumnToken(new ReadOnlySpan<char>(normalized, 0, len), col.DataType, col.AllowDBNull ?? true);
//                    }
//                }
//                tokens[i] = token;
//            }
//            if (normalized is not null) ArrayPool<char>.Shared.Return(normalized);
//        }

//        /// <summary>
//        /// Nominate a token for the supplied column
//        /// </summary>
//        protected virtual int GetColumnToken(string name, Type? type, bool isNullable)
//            => GetColumnToken(name.AsSpan(), type, isNullable);

//        /// <summary>
//        /// Nominate a token for the supplied column
//        /// </summary>
//        protected abstract int GetColumnToken(ReadOnlySpan<char> name, Type? type, bool isNullable);
//    }
//}
