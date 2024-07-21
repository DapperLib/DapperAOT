namespace Dapper.Aot.Generated
{
    /// <summary>
    /// Contains helpers to properly handle <see href="https://github.com/DapperLib/Dapper/blob/main/Dapper/DbString.cs"/>
    /// </summary>
#if !DAPPERAOT_INTERNAL
    file
#endif
    static class DbStringHelpers
    {
        public static void ConfigureDbStringDbParameter(
            global::System.Data.Common.DbParameter dbParameter,
            global::Dapper.DbString? dbString)
        {
            if (dbString is null)
            {
                dbParameter.Value = global::System.DBNull.Value;
                return;
            }

            // repeating logic from Dapper:
            // https://github.com/DapperLib/Dapper/blob/52160dc44699ec7eb5ad57d0dddc6ded4662fcb9/Dapper/DbString.cs#L71
            if (dbString.Length == -1 && dbString.Value is not null && dbString.Value.Length <= global::Dapper.DbString.DefaultLength)
            {
                dbParameter.Size = global::Dapper.DbString.DefaultLength;
            }
            else
            {
                dbParameter.Size = dbString.Length;
            }

            dbParameter.DbType = dbString switch
            {
                { IsAnsi: true, IsFixedLength: true } => global::System.Data.DbType.AnsiStringFixedLength,
                { IsAnsi: true, IsFixedLength: false } => global::System.Data.DbType.AnsiString,
                { IsAnsi: false, IsFixedLength: true } => global::System.Data.DbType.StringFixedLength,
                { IsAnsi: false, IsFixedLength: false } => global::System.Data.DbType.String,
                _ => dbParameter.DbType
            };

            dbParameter.Value = dbString.Value as object ?? global::System.DBNull.Value;
        }
    }
}