using Dapper;
using System.Data.Common;
using System.Linq;

[module: DapperAot]
public static class Foo
{
    static void EnumerableExtensions(DbConnection db)
    {
        _ = db.Query<int>("abc").ToList(); // should spot
        _ = db.Query<int>("abc").AsList(); // OK

        _ = db.Query<int>("abc").First(); // should spot
        _ = db.Query<int>("abc").Single(); // should spot
        _ = db.Query<int>("abc").FirstOrDefault(); // should spot
        _ = db.Query<int>("abc").SingleOrDefault(); // should spot
    }
}