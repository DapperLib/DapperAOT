using Dapper;
using System.Data;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, SqlMapper.ICustomQueryParameter tvp)
    {
        // ICustomQueryParameter members add themselves (TVPs are the common case); a member
        // typed as the interface, a class, and a struct - the struct needs no null test
        _ = connection.Query<int>("select count(1) from @ids", new { ids = tvp });
        _ = connection.Query<int>("select count(1) from @ids where Id = @id", new { ids = new CustomClass(), id = 42 });
        _ = connection.Query<int>("select count(1) from @ids", new { ids = new CustomStruct() });

        // skipped: PostProcess reads output parameters back by index, and a self-binding
        // member contributes an unknowable number of parameters before them
        _ = connection.Execute("exec SomeProc @ids, @total out", new WithOutput { ids = new CustomClass() });

        // skipped: multi-exec batch reuse updates parameters in-place
        _ = connection.Execute("exec SomeProc @ids", new[] { new WithCustom(), new WithCustom() });
    }

    public class CustomClass : SqlMapper.ICustomQueryParameter
    {
        public void AddParameter(IDbCommand command, string name) { }
    }
    public struct CustomStruct : SqlMapper.ICustomQueryParameter
    {
        public void AddParameter(IDbCommand command, string name) { }
    }
    public class WithOutput
    {
        public CustomClass ids { get; set; }
        [DbValue(Direction = ParameterDirection.Output)]
        public int total { get; set; }
    }
    public class WithCustom
    {
        public CustomClass ids { get; set; }
    }
}
