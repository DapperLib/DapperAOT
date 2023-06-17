using Dapper;
using System.Data.Common;



public static class SomeCode
{
    [DapperAot(true)]
    public static void WithDapper(DbConnection connection, object args)
    {
        // expect non-constant SQL
        connection.Execute(System.Console.ReadLine());

        // expect warning that BindByName should be specified
        // expect tuple-type results not supported
        connection.QueryFirst<(int Id, string Name)>("blah");

        // expect warning that BindByName should be specified
        // expect tuple-type params not supported
        connection.Execute("blah", (Id: 42, Name: "abc"));

        // expect warning that args type unknown
        connection.Execute("blah", args);

        // no warning expected, this is fine
        connection.Execute("blah");
    }

    public class InternalNesting
    {
        [DapperAot(true)]
        public static void WithDapper(DbConnection connection, object args)
        {
            // should be fine
            connection.QueryFirst<InternalNesting.SomePublicType>("blah");
            connection.Execute("blah", new InternalNesting.SomePublicType { Id = 42 });

            // should be fine
            connection.QueryFirst<InternalNesting.SomeInternalType>("blah");
            connection.Execute("blah", new InternalNesting.SomeInternalType { Id = 42 });

            // should fail inaccessible
            connection.QueryFirst<InternalNesting.SomePrivateType>("blah");
            connection.Execute("blah", new InternalNesting.SomePrivateType { Id = 42 });

            // should fail (not accessible)
            connection.QueryFirst<InternalNesting.SomeProtectedType>("blah");
            connection.Execute("blah", new InternalNesting.SomeProtectedType { Id = 42 });

            // should fail (not accessible)
            connection.QueryFirst<InternalNesting.SomePrivateProtectedType>("blah");
            connection.Execute("blah", new InternalNesting.SomePrivateProtectedType { Id = 42 });

            // should be fine
            connection.QueryFirst<InternalNesting.SomeProtectedInternalType>("blah");
            connection.Execute("blah", new InternalNesting.SomeProtectedInternalType { Id = 42 });
        }

        public class SomePublicType
        {
            public int Id { get; set; }
        }
        internal class SomeInternalType
        {
            public int Id { get; set; }
        }
        private class SomePrivateType
        {
            public int Id { get; set; }
        }
        protected class SomeProtectedType
        {
            public int Id { get; set; }
        }
        private protected class SomePrivateProtectedType
        {
            public int Id { get; set; }
        }
        protected internal class SomeProtectedInternalType
        {
            public int Id { get; set; }
        }
    }

    private class PrivateNesting
    {
        [DapperAot(true)]
        public static void WithDapper(DbConnection connection, object args)
        {
            // should fail (not accessible)
            connection.QueryFirst<PrivateNesting.SomePublicType>("blah");
            connection.Execute("blah", new InternalNesting.SomePublicType { Id = 42 });
        }

        public class SomePublicType
        {
            public int Id { get; set; }
        }
    }

    [DapperAot(true)]
    public static void WithDapper<T>(DbConnection connection, T args)
    {
        // expect warning about generic
        connection.Execute("blah", args);

        // expect warning about generic
        connection.Query<T>("blah");
    }

    [DapperAot(true), BindTupleByName]
    public static void WithDapperAndBindByName(DbConnection connection)
    {
        // expect tuple-type results not supported
        connection.QueryFirst<(int Id, string Name)>("blah");

        // expect tuple-type params not supported
        connection.Execute("blah", (Id: 42, Name: "abc"));
    }

    [DapperAot(false), BindTupleByName]
    public static void WithoutDapper(DbConnection connection)
    {
        // do not expect non-constant SQL warning; legacy Dapper is fine with this
        connection.Execute(System.Console.ReadLine());

        // expect warning that dapper doesn't support bind-by-name tuple results
        connection.QueryFirst<(int Id, string Name)>("blah");
    }
}