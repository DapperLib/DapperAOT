using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;

// Which overload does an ordinary Dapper call site bind to, if a generic-args overload is added
// alongside the existing object?-based one? Nothing here talks to a database; the only question is
// what the compiler does with real-looking call sites.

public sealed class Customer { public int Id { get; set; } }

public sealed class CustomerArgs { public int Id { get; set; } }

public sealed class DynamicParametersLike { }

public sealed class FakeConnection : IDbConnection
{
    public string ConnectionString { get; set; } = "";
    public int ConnectionTimeout => 0;
    public string Database => "";
    public ConnectionState State => ConnectionState.Open;
    public IDbTransaction BeginTransaction() => throw new NotSupportedException();
    public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
    public void ChangeDatabase(string databaseName) { }
    public void Close() { }
    public IDbCommand CreateCommand() => throw new NotSupportedException();
    public void Open() { }
    public void Dispose() { }
}

public static class Existing
{
    public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object? param = null)
    {
        Log.Bound("Query<T>(string, object?)          [today]");
        return [];
    }

    public static int Execute(this IDbConnection cnn, string sql, object? param = null)
    {
        Log.Bound("Execute(string, object?)           [today]");
        return 0;
    }

    // Dapper's dynamic-returning Query: no explicit type argument at the call site.
    public static IEnumerable<object> Query(this IDbConnection cnn, string sql, object? param = null)
    {
        Log.Bound("Query(string, object?) -> dynamic  [today]");
        return [];
    }

    public static object? ExecuteScalar(this IDbConnection cnn, string sql, object? param = null)
    {
        Log.Bound("ExecuteScalar(string, object?)     [today]");
        return null;
    }
}

public static class Candidates
{
    // A: two type parameters, TResult only in the return position.
    public static IEnumerable<TResult> Query<TResult, TArgs>(this IDbConnection cnn, string sql, TArgs param)
    {
        Log.Bound("Query<TResult, TArgs>              [candidate A]");
        return [];
    }

    // B: one type parameter, inferable from the argument. No OverloadResolutionPriority, to find out
    // whether ordinary overload resolution already prefers it.
    public static int Execute<TArgs>(this IDbConnection cnn, string sql, TArgs param)
    {
        Log.Bound("Execute<TArgs>(string, TArgs)      [candidate B]");
        return 0;
    }

    // C: the same trick on the dynamic-returning Query, which also has no explicit type argument.
    public static IEnumerable<object> Query<TArgs>(this IDbConnection cnn, string sql, TArgs param)
    {
        Log.Bound("Query<TArgs>(string, TArgs)        [candidate C]");
        return [];
    }

    public static object? ExecuteScalar<TArgs>(this IDbConnection cnn, string sql, TArgs param)
    {
        Log.Bound("ExecuteScalar<TArgs>(string,TArgs) [candidate D]");
        return null;
    }
}

public static class Log
{
    public static void Bound(string what) => Console.WriteLine($"    -> {what}");
}

/// <summary>
/// Which overload does an ordinary Dapper call site bind to, if a generic-args overload is added
/// alongside the existing object?-based one -- and what does the argument object actually cost?
/// </summary>
/// <remarks>
/// Behind the "the args object is not the prize" section of notes/provider-specialization.md. The
/// headline is that an explicit type argument (Query&lt;Customer&gt;) excludes a two-parameter
/// overload from candidacy outright, because C# has no partial inference and an anonymous type
/// cannot be named -- so the dominant Dapper read shape cannot reach one by construction.
/// </remarks>
internal static class OverloadBinding
{
    public static void Run()
    {
        var cnn = new FakeConnection();
        object? nullArgs = null;
        object boxedArgs = new CustomerArgs { Id = 1 };
        var bag = new DynamicParametersLike();

        Say("1. Query<Customer>(sql, new { id })      -- the dominant Dapper read shape");
        _ = cnn.Query<Customer>("select ...", new { id = 1 });

        Say("2. Execute(sql, new { id })              -- no explicit type argument anywhere");
        _ = cnn.Execute("update ...", new { id = 1 });

        Say("3. Execute(sql)                          -- no args");
        _ = cnn.Execute("update ...");

        Say("4. Execute(sql, null)                    -- null cannot infer TArgs");
        _ = cnn.Execute("update ...", null);

        Say("5. Execute(sql, objectTypedLocal)        -- static type is object");
        _ = cnn.Execute("update ...", boxedArgs);

        Say("6. Execute(sql, typedArgsClass)          -- an ordinary named class");
        _ = cnn.Execute("update ...", new CustomerArgs { Id = 1 });

        Say("7. Execute(sql, dynamicParametersLike)   -- the bag shape Dapper handles specially");
        _ = cnn.Execute("update ...", bag);

        Say("8. Execute(sql, nullObjectLocal)         -- null in an object?-typed local");
        _ = cnn.Execute("update ...", nullArgs);

        Say("9. Query(sql, new { id })                -- dynamic result, no explicit type argument");
        _ = cnn.Query("select ...", new { id = 1 });

        Say("10. ExecuteScalar(sql, new { id })        -- same shape again");
        _ = cnn.ExecuteScalar("select ...", new { id = 1 });

        Console.WriteLine();
        Console.WriteLine("What does the args object actually cost?");
        Measure("new { id = 42 }", static () => new { id = 42 });
        Measure("new { id = 42, name = \"x\" }", static () => new { id = 42, name = "x" });
        Measure("new CustomerArgs()", static () => new CustomerArgs { Id = 42 });

    }

    private static void Say(string what)
    {
        Console.WriteLine();
        Console.WriteLine(what);
    }

    private static void Measure<T>(string what, Func<T> make)
    {
        _ = make(); // JIT and warm up
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
            _ = make();
        var after = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"    {what,-28} {(after - before) / 1000.0,6:0.0} B per instance");
    }
}
