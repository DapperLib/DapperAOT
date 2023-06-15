using System.Data.Common;
using System.Threading.Tasks;

public static class Foo
{
    // the point of this test is to check that we do not emit DAP005
    static async Task SomeCode(DbConnection connection, string bar)
    {
        connection.ConnectionString = bar;
        await connection.OpenAsync();
        connection.Close();
    }
}