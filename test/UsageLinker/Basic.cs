#pragma warning disable CA1050, CA1822
using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

public static class Foo
{
    [DapperAot(false)]
    public static async Task SomeCode()
    {
        string bar = "das";
        Customer customer = null!;
        new NotDapper().Execute("abc");
        DbConnection foo = null!;
        foo.Execute("abc");
        SqlMapper.Execute(foo, "abc");

        await foo.ExecuteAsync("abc");
        await SqlMapper.ExecuteAsync(foo, "abc");

        await foo.ExecuteAsync("abc", customer);

        foo.Query<int>("abc");
        await foo.QueryAsync<int>("abc");

        await foo.QueryAsync<int>("abc", new { Foo = 12, bar });
        await foo.QueryAsync<int>("def", new { Foo = 12, bar });

        foo.QueryFirst<Customer>("def", new { Foo = 12, bar });
    }
    class NotDapper { public void Execute(string _) { } }
    public class Customer
    {
        public int X { get; set; }
        public string? Y;
    }
}


