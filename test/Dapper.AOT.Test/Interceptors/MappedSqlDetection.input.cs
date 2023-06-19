using Dapper;
using System.Data;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var obj = new SomeArg();

        // fine, detect A
        connection.Execute("foo @a", obj);

        // fine, but B not detected
        connection.Execute("foo @b", obj);

        // fine, detect B
        connection.Execute("foo @f", obj);

        // fine, but C not detected
        connection.Execute("foo @c", obj);

        // fine, detect D
        connection.Execute("foo @d", obj);

        // fine, detect E
        connection.Execute("return 42", obj);

        // send X, detect problems with duplicate row counts and row count vs db value
        connection.Execute("select @X @Y", new SomeOtherArg());
    }

    public class SomeArg
    {
        public int A { get; set; }
        [DbValue(Name = "f", DbType = DbType.AnsiString, Size = 200)]
        public string B { get; set; }
        internal string C { get; set; }
        [DbValue(Precision = 12, Scale = 42)]
        internal decimal D { get; set; }
        [DbValue(Direction = ParameterDirection.ReturnValue)]
        internal string E { get; set; }
        [RowCount]
        internal int RowCount { get; set; }
    }


    public class SomeOtherArg
    {
        public int X { get; set; }
        [RowCount]
        public int RowCount;
        [RowCount, DbValue(Name = "Y")]
        public int DuplicateToCauseProblems;
    }
}