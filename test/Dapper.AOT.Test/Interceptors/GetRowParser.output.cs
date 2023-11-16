#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\GetRowParser.input.cs", 12, 29)]
        internal static global::System.Func<global::System.Data.Common.DbDataReader, global::HazNameId> GetRowParser0(this global::System.Data.Common.DbDataReader reader, global::System.Type? concreteType, int startIndex, int length, bool returnNullIfFirstMissing)
        {
            // TypedResult, GetRowParser
            // returns data: global::HazNameId
            global::System.Diagnostics.Debug.Assert(concreteType is null);

            return RowFactory0.Instance.GetRowParser(reader, startIndex, length, returnNullIfFirstMissing);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\GetRowParser.input.cs", 32, 29)]
        internal static global::System.Func<global::System.Data.Common.DbDataReader, dynamic> GetRowParser1(this global::System.Data.Common.DbDataReader reader, global::System.Type? concreteType, int startIndex, int length, bool returnNullIfFirstMissing)
        {
            // TypedResult, GetRowParser
            // returns data: dynamic
            global::System.Diagnostics.Debug.Assert(concreteType is null);

            return global::Dapper.RowFactory.Inbuilt.Dynamic.GetRowParser(reader, startIndex, length, returnNullIfFirstMissing);

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::HazNameId>
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
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 0 : 2; // two tokens for right-typed and type-flexible
                            break;
                        case 926444256U when NormalizedEquals(name, "id"):
                            token = type == typeof(int) ? 1 : 3;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::HazNameId Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::HazNameId result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 2:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 1:
                            result.Id = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            result.Id = GetValue<int>(reader, columnOffset);
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