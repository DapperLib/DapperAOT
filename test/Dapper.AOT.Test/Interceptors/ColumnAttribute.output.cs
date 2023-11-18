#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ColumnAttribute.input.cs", 13, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.MyType> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.MyType
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory0.Instance);

        }

        private class CommonCommandFactory<T> : global::Dapper.CommandFactory<T>
        {
            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)
            {
                var cmd = base.GetCommand(connection, sql, commandType, args);
                // apply special per-provider command initialization logic for OracleCommand
                if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
                {
                    cmd0.BindByName = true;
                    cmd0.InitialLONGFetchSize = -1;

                }
                return cmd;
            }

        }

        private static readonly CommonCommandFactory<object?> DefaultCommandFactory = new();

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.MyType>
        {
            internal static readonly RowFactory0 Instance = new();
            private RowFactory0() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    int token = -1;
                    var name = reader.GetName(columnOffset);
                    var type = reader.GetFieldType(columnOffset);
                    switch (NormalizedHash(name))
                    {
                        case 3826002220U when NormalizedEquals(name, "a"):
                            token = type == typeof(int) ? 0 : 5; // two tokens for right-typed and type-flexible
                            break;
                        case 1982833670U when NormalizedEquals(name, "dbvalueb"):
                            token = type == typeof(int) ? 1 : 6;
                            break;
                        case 315420707U when NormalizedEquals(name, "standardcolumnc"):
                            token = type == typeof(int) ? 2 : 7;
                            break;
                        case 3687481269U when NormalizedEquals(name, "explicitcolumnd"):
                            token = type == typeof(int) ? 3 : 8;
                            break;
                        case 2033166527U when NormalizedEquals(name, "dbvaluee"):
                            token = type == typeof(int) ? 4 : 9;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Foo.MyType Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.MyType result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.A = reader.GetInt32(columnOffset);
                            break;
                        case 5:
                            result.A = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            result.B = reader.GetInt32(columnOffset);
                            break;
                        case 6:
                            result.B = GetValue<int>(reader, columnOffset);
                            break;
                        case 2:
                            result.C = reader.GetInt32(columnOffset);
                            break;
                        case 7:
                            result.C = GetValue<int>(reader, columnOffset);
                            break;
                        case 3:
                            result.D = reader.GetInt32(columnOffset);
                            break;
                        case 8:
                            result.D = GetValue<int>(reader, columnOffset);
                            break;
                        case 4:
                            result.E = reader.GetInt32(columnOffset);
                            break;
                        case 9:
                            result.E = GetValue<int>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }


    }
}
namespace System.Runtime.CompilerServices
{
    // this type is needed by the compiler to implement interceptors - it doesn't need to
    // come from the runtime itself, though

    [global::System.Diagnostics.Conditional("DEBUG")] // not needed post-build, so: evaporate
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : global::System.Attribute
    {
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }
    }
}