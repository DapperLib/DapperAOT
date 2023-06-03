using Dapper;
using System.Data.Common;

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

class NotDapper { public void Execute(string _) { } }
class Customer
{
    public int X { get; set; }
    public string Y;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class InterceptsLocationAttribute : Attribute
    {
        public InterceptsLocationAttribute(string x, int y, int z) { }
    }
}