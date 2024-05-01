namespace Dapper.Aot.Generated
{
    /// <summary>
    /// Contains helpers to properly handle <see href="https://github.com/DapperLib/Dapper/blob/main/Dapper/DbString.cs"/>
    /// </summary>
    static class DbStringHelpers
    {
        public static global::System.Data.Common.DbParameter ConvertToDbParameter(
            global::System.Data.Common.DbParameter dbParameter,
            global::Dapper.DbString? dbString)
        {
            if (dbString is null)
            {
                dbParameter.Value = global::System.DBNull.Value;
                return dbParameter;
            }

            dbParameter.Size = dbString switch
            {
                { Value: null } => 0,
                { IsAnsi: false } => global::System.Text.Encoding.ASCII.GetByteCount(dbString.Value),
                { IsAnsi: true } => global::System.Text.Encoding.Default.GetByteCount(dbString.Value),
                _ => default
            };
            dbParameter.DbType = dbString switch
            {
                { IsAnsi: true, IsFixedLength: true } => global::System.Data.DbType.AnsiStringFixedLength,
                { IsAnsi: true, IsFixedLength: false } => global::System.Data.DbType.AnsiString,
                { IsAnsi: false, IsFixedLength: true } => global::System.Data.DbType.StringFixedLength,
                { IsAnsi: false, IsFixedLength: false } => global::System.Data.DbType.String,
                _ => dbParameter.DbType
            };

            dbParameter.Value = dbString.Value as object ?? global::System.DBNull.Value;

            return dbParameter;
        }
    }
}