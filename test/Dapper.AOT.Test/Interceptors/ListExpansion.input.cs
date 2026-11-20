using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // list expansion: the member binds via Dapper's own PackListParameters, which owns
        // the whole in-list contract (rewrite, empty form, padding, split, DbString items)
        _ = connection.Query<int>("select Id from Customers where Id in @ids", new { ids = new[] { 1, 2, 3 } });
        _ = connection.Query<int>("select Id from Customers where Id in @ids and Region = @region", new { ids = new List<int> { 1 }, region = "north" });
        _ = connection.Query<int>("select Name from Customers where Name in @names", new { names = new[] { "a", "b" } });

        // not expanded: Dapper's type map binds byte[] as one DbType.Binary value
        _ = connection.Execute("update Customers set Photo = @photo where Id = @id", new { photo = new byte[] { 1, 2 }, id = 1 });
        _ = connection.Execute("update Customers set Photo = @photo where Id = @id", new[] { new WithPhoto(), new WithPhoto() });
        _ = connection.Execute("select @hash = HASHBYTES('SHA2_256', Photo) from Customers where Id = @id", new WithHash { id = 1 });

        // skipped: expansion adds a runtime-variable number of parameters, so the by-index
        // read-back of the output parameter cannot be trusted; stays on vanilla Dapper
        _ = connection.Execute("declare @dummy int; select @total = count(1) from Customers where Id in @ids", new WithOutput { ids = new[] { 1, 2 } });

        // skipped: multi-exec batch reuse updates parameters in-place, which cannot re-expand
        // a list whose size changed between items; stays on vanilla Dapper
        _ = connection.Execute("insert Audit (Id) select v from @ids", new[] { new WithList(), new WithList() });
    }

    public class WithOutput
    {
        public int[] ids { get; set; } = System.Array.Empty<int>();
        [DbValue(Direction = ParameterDirection.Output)]
        public int total { get; set; }
    }
    public class WithList
    {
        public int[] ids { get; set; } = System.Array.Empty<int>();
    }
    public class WithPhoto
    {
        public byte[] photo { get; set; } = System.Array.Empty<byte>();
        public int id { get; set; }
    }
    public class WithHash
    {
        public int id { get; set; }
        [DbValue(Direction = ParameterDirection.Output, Size = 32)]
        public byte[] hash { get; set; } = System.Array.Empty<byte>();
    }
}
