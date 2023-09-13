using Dapper;
using SomeApp;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

[module: DapperAot]
[module: CommandFactory<GlobalFetchSizeCommandFactory<object>>]

namespace SomeApp
{
    public static class Foo
    {
        static List<SomeQueryType> SomeCode(DbConnection connection, string bar)
            => connection.Query<SomeQueryType>("def", new { Foo = 12, bar }).AsList();
    }
    public class SomeQueryType { public int Dummy { get; set; } }

    public static class GlobalSettings
    {
        public static int DefaultFetchSize { get; set; } = 131072; // from Oracle docs
    }
    public interface IDynamicFetchSize
    {
        int FetchSize { get; }
    }

    internal abstract class GlobalFetchSizeCommandFactory<T> : CommandFactory<T>
    {
        public override DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, T args)
        {
            var cmd = base.GetCommand(connection, sql, commandType, args);
            if (cmd is OracleCommand oracle)
            {
                oracle.FetchSize = args is IDynamicFetchSize d ? d.FetchSize : GlobalSettings.DefaultFetchSize;
            }
            return cmd;
        }
    }

}