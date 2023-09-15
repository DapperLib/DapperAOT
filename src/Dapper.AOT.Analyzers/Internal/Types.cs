namespace Dapper.Internal;

internal static class Types
{
    public const string
        BindTupleByNameAttribute = nameof(BindTupleByNameAttribute),
        DapperAotAttribute = nameof(DapperAotAttribute),
        DbValueAttribute = nameof(DbValueAttribute),
        RowCountAttribute = nameof(RowCountAttribute),
        CacheCommandAttribute = nameof(CacheCommandAttribute),
        IncludeLocationAttribute = nameof(IncludeLocationAttribute),
        SqlSyntaxAttribute = nameof(SqlSyntaxAttribute),
        EstimatedRowCountAttribute = nameof(EstimatedRowCountAttribute),
        CommandPropertyAttribute = nameof(CommandPropertyAttribute),
        DynamicParameters = nameof(DynamicParameters),
        IDynamicParameters = nameof(IDynamicParameters),
        SqlMapper = nameof(SqlMapper);
}
