#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TypeHandlerRegistration.input.cs", 20, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Appointment> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, BindResultsByName, KnownParameters
            // takes parameter: global::Appointment
            // parameter map: Day
            // returns data: global::Appointment
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryBuffered((global::Appointment)param!, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TypeHandlerRegistration.input.cs", 25, 24)]
        internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::Appointment
            // parameter map: Day MovedTo
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).Execute((global::Appointment)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TypeHandlerRegistration.input.cs", 29, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Invoice> Query2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, BindResultsByName, KnownParameters
            // takes parameter: global::Invoice
            // parameter map: Total
            // returns data: global::Invoice
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).QueryBuffered((global::Invoice)param!, RowFactory1.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TypeHandlerRegistration.input.cs", 33, 24)]
        internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::AppointmentOut
            // parameter map: Day
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory3.Instance).Execute((global::AppointmentOut)param!);

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Appointment>
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
                        case 3830391293U when NormalizedEquals(name, "day"):
                            token = 2; // type-handler: the handler decides
                            break;
                        case 2725939961U when NormalizedEquals(name, "movedto"):
                            token = 3; // type-handler: the handler decides
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Appointment Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Appointment result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.Day = TypeHandler0.Parse(reader, columnOffset, 0);
                            break;
                        case 2:
                            result.Day = TypeHandler0.Parse(reader, columnOffset, 0);
                            break;
                        case 1:
                            result.MovedTo = reader.IsDBNull(columnOffset) ? (global::LocalDate?)null : TypeHandler0.Parse(reader, columnOffset, 0);
                            break;
                        case 3:
                            result.MovedTo = reader.IsDBNull(columnOffset) ? (global::LocalDate?)null : TypeHandler0.Parse(reader, columnOffset, 0);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class RowFactory1 : global::Dapper.RowFactory<global::Invoice>
        {
            internal static readonly RowFactory1 Instance = new();
            private RowFactory1() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    int token = -1;
                    var name = reader.GetName(columnOffset);
                    var type = reader.GetFieldType(columnOffset);
                    switch (NormalizedHash(name))
                    {
                        case 80777981U when NormalizedEquals(name, "total"):
                            token = 1; // type-handler: the handler decides
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Invoice Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Invoice result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.Total = TypeHandler1.Parse(reader, columnOffset, 0);
                            break;
                        case 1:
                            result.Total = TypeHandler1.Parse(reader, columnOffset, 0);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<global::Appointment>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Appointment args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Day";
                p.Direction = global::System.Data.ParameterDirection.Input;
                TypeHandler0.SetValue(p, args.Day);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Appointment args)
            {
                var ps = cmd.Parameters;
                TypeHandler0.SetValue(ps[0], args.Day);

            }

        }

        private sealed class CommandFactory1 : CommonCommandFactory<global::Appointment>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Appointment args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Day";
                p.Direction = global::System.Data.ParameterDirection.Input;
                TypeHandler0.SetValue(p, args.Day);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "MovedTo";
                p.Direction = global::System.Data.ParameterDirection.Input;
                if (args.MovedTo is null) TypeHandler0.SetNullValue(p);
                else TypeHandler0.SetValue(p, args.MovedTo.GetValueOrDefault());
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Appointment args)
            {
                var ps = cmd.Parameters;
                TypeHandler0.SetValue(ps[0], args.Day);
                if (args.MovedTo is null) TypeHandler0.SetNullValue(ps[1]);
                else TypeHandler0.SetValue(ps[1], args.MovedTo.GetValueOrDefault());

            }

        }

        private sealed class CommandFactory2 : CommonCommandFactory<global::Invoice>
        {
            internal static readonly CommandFactory2 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Invoice args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Total";
                p.Direction = global::System.Data.ParameterDirection.Input;
                TypeHandler1.SetValue(p, args.Total);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Invoice args)
            {
                var ps = cmd.Parameters;
                TypeHandler1.SetValue(ps[0], args.Total);

            }

        }

        private sealed class CommandFactory3 : CommonCommandFactory<global::AppointmentOut>
        {
            internal static readonly CommandFactory3 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::AppointmentOut args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Day";
                p.Direction = global::System.Data.ParameterDirection.Output;
                p.Value = global::System.DBNull.Value;
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::AppointmentOut args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = global::System.DBNull.Value;

            }
            public override bool RequirePostProcess => true;

            public override void PostProcess(in global::Dapper.UnifiedCommand cmd, global::AppointmentOut args, int rowCount)
            {
                var ps = cmd.Parameters;
                args.Day = TypeHandler0.Parse(ps[0]);
                base.PostProcess(in cmd, args, rowCount);

            }

        }


        private static readonly global::Dapper.IDbValueHandler<global::LocalDate> TypeHandler0 = new global::LocalDateHandler();
        private static readonly global::Dapper.IDbValueHandler<global::Money> TypeHandler1 = new global::Dapper.Aot.Generated.VanillaTypeHandler<global::Money>(new global::MoneyHandler());

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
