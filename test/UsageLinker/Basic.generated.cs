//#nullable enable

//using Microsoft.Data.SqlClient;
//using Oracle.ManagedDataAccess.Client;
//using System;
//using System.Data.Common;
//using System.Globalization;
//using System.IO;

//file static class DapperGeneratedInterceptors
//{
//#pragma warning disable CS0618
//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 15, 13)]
//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 16, 19)]
//    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        // Execute
//        throw new global::System.NotImplementedException("lower your expectations");
//    }

//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 18, 19)]
//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 19, 25)]
//    internal static global::System.Threading.Tasks.Task<int> ExecuteAsync1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        // Execute, Async
//        throw new global::System.NotImplementedException("lower your expectations");
//    }

//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 21, 19)]
//    internal static global::System.Threading.Tasks.Task<int> ExecuteAsync2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        // Execute, Async, HasParameters
//        // takes parameter: global::Foo.Customer
//        throw new global::System.NotImplementedException("lower your expectations");
//    }

//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 23, 13)]
//    internal static global::System.Collections.Generic.IEnumerable<int> Query3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        // Query, TypedResult, Buffered
//        // returns data: int
//        throw new global::System.NotImplementedException("lower your expectations");
//    }

//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 24, 19)]
//    internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<int>> QueryAsync4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        // Query, Async, TypedResult, Buffered
//        // returns data: int
//        throw new global::System.NotImplementedException("lower your expectations");
//    }

//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 26, 19)]
//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 27, 19)]
//    internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<int>> QueryAsync5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);

//        //        // Query, Async, TypedResult, HasParameters, Buffered
//        //        // takes parameter: <anonymous type: int Foo, string bar>
//        //        // returns data: int
//        //        return global::Dapper.Internal.InterceptorHelpers.QueryBufferedAsync(
//        //            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn),
//        //            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
//        //            param, sql, commandTimeout,
//        //            global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SequentialAccess,
//        //            static () => new { Foo = default(int), bar = default(string) },
//        //            static args => (args.Foo, args.bar),
//        //            static (cmd, args) =>
//        //            {
//        //                cmd.CommandType = global::System.Data.CommandType.Text;
//        //                var p = cmd.CreateParameter();
//        //                p.ParameterName = nameof(args.Foo);
//        //                p.DbType = global::System.Data.DbType.Int32;
//        //                p.Value = args.Foo;
//        //                cmd.Parameters.Add(p);

//        //                p = cmd.CreateParameter();
//        //                p.ParameterName = nameof(args.bar);
//        //                p.DbType = global::System.Data.DbType.String;
//        //                p.Value = args.bar ?? (object)DBNull.Value;
//        //                cmd.Parameters.Add(p);
//        //            },
//        //            (reader, tokens, fieldOffset) => { },
//        //            (reader, tokens, fieldOffset) => reader.GetInt32(fieldOffset),
//        //            global::System.Threading.CancellationToken.None
//        //        );
//        throw new global::System.NotImplementedException("lower your expectations");
//    }
//    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("D:\\Code\\DapperAOT\\test\\UsageLinker\\Basic.cs", 29, 13)]
//    internal static unsafe global::Foo.Customer QueryFirst6(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
//    {
//        // Query, TypedResult, HasParameters, Buffered, First
//        // takes parameter: <anonymous type: int Foo, string bar>
//        // returns data: global::Foo.Customer
//        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
//        global::System.Diagnostics.Debug.Assert(param is not null);

//        return global::Dapper.Internal.InterceptorHelpers.UnsafeQueryFirst(
//            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn), sql,
//            global::Dapper.Internal.InterceptorHelpers.Reshape(param,
//                static () => new { Foo = default(int), bar = default(string) },
//                static args => (args!.Foo, args!.bar)
//            ),
//            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
//            commandTimeout,
//            &CommandBuilder0,
//            &ColumnTokenizer0,
//            &RowReader0
//        );
//    }

//    private static void CommandBuilder0(global::System.Data.Common.DbCommand cmd, (int Foo, string? bar) args)
//    {
//        InitCommand(cmd);
//        cmd.CommandType = global::System.Data.CommandType.Text;
//        var p = cmd.CreateParameter();
//        p.ParameterName = nameof(args.Foo);
//        p.DbType = global::System.Data.DbType.Int32;
//        p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.Foo);
//        cmd.Parameters.Add(p);

//        p = cmd.CreateParameter();
//        p.ParameterName = nameof(args.bar);
//        p.DbType = global::System.Data.DbType.String;
//        p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.bar);
//        cmd.Parameters.Add(p);
//    }

//    private static void ColumnTokenizer0(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int fieldOffset)
//    {
//        for (int i = 0; i < tokens.Length; i++)
//        {
//            int token = -1;
//            var name = reader.GetName(fieldOffset);
//            var type = reader.GetFieldType(fieldOffset);
//            if (!string.IsNullOrWhiteSpace(name))
//            {
//                switch (global::Dapper.Internal.StringHashing.NormalizedHash(name))
//                {
//                    case 4245442695 when global::Dapper.Internal.StringHashing.NormalizedEquals(name, "x"):
//                        token = type == typeof(int) ? 0 : 2;
//                        break;
//                    case 4228665076 when global::Dapper.Internal.StringHashing.NormalizedEquals(name, "y"):
//                        token = type == typeof(string) ? 1 : 3;
//                        break;
//                }
//            }
//            tokens[i] = token;
//            fieldOffset++;
//        }
//    }

//    private static Foo.Customer RowReader0(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int fieldOffset)
//    {
//        var obj = new Foo.Customer();
//        foreach (var token in tokens)
//        {
//            if (!reader.IsDBNull(fieldOffset))
//            {
//                switch (token)
//                {
//                    case 0:
//                        obj.X = reader.GetInt32(fieldOffset);
//                        break;
//                    case 1:
//                        obj.Y = reader.GetString(fieldOffset);
//                        break;
//                    // same, but with type conversions
//                    case 2:
//                        obj.X = global::Dapper.Internal.InterceptorHelpers.GetValue<int>(reader, fieldOffset);
//                        break;
//                    case 3:
//                        obj.Y = global::Dapper.Internal.InterceptorHelpers.GetValue<string>(reader, fieldOffset);
//                        break;
//                }
//            }

//            fieldOffset++;
//        }
//        return obj;
//    }

//    private static void InitCommand(DbCommand cmd)
//    {
//        if (cmd is OracleCommand t0)
//        {
//            t0.BindByName = true;
//            t0.InitialLONGFetchSize = -1;
//        }
//    }

//#pragma warning restore CS0618
//}
//namespace System.Runtime.CompilerServices
//{
//    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
//    sealed file class InterceptsLocationAttribute : Attribute
//    {
//        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
//        {
//            _ = path;
//            _ = lineNumber;
//            _ = columnNumber;
//        }
//    }
//}