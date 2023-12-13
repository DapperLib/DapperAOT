using Dapper;
using System;
using System.Data.Common;
using System.Threading.Tasks;

[DapperAot(enabled: true)]
public static class UsersSqlQueries
{
    public sealed class UserIncrementParams
    {
        [DbValue(Name = "userId")]
        public int UserId { get; set; }

        [DbValue(Name = "date", DbType = System.Data.DbType.Date)]
        public DateTime Date { get; set; }
    }

    public static async Task IncrementAsync(DbConnection connection, int userId)
    {
        var date = DateTime.Today;

        await connection.ExecuteAsync("""
            UPDATE [dbo].[table]
            SET [column] = ([column] + 1) 
            WHERE [Id] = @userId and [Date] = @date
            """, new UserIncrementParams() { UserId = userId, Date = date });
    }
}