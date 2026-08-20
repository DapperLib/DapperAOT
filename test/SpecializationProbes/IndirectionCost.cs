using System;
using System.Data.Common;
using System.Diagnostics;

public class FakeDb : DbConnection
{
    public override string ConnectionString { get; set; } = "";
    public override string Database => "";
    public override string DataSource => "";
    public override string ServerVersion => "";
    public override System.Data.ConnectionState State => System.Data.ConnectionState.Open;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel il) => null!;
    protected override DbCommand CreateDbCommand() => null!;
}

/// <summary>
/// Does either indirection allocate per call, and what does the delegate fallback cost?
/// </summary>
/// <remarks>
/// Behind the "passing the binder in" section of notes/provider-specialization.md. Two results
/// worth knowing before choosing: neither indirection allocates, and the function pointer measured
/// <em>slower</em> than the delegate -- most likely because the JIT can speculatively inline through
/// a delegate with a stable target and cannot do so for a pointer arriving as a parameter.
/// <para>Note also that the function pointer needs <c>AllowUnsafeBlocks</c> in the <em>consumer's</em>
/// project: without it, generated code using one fails with CS0214, and that is a compilation-wide
/// switch a generated file cannot opt into on its own.</para>
/// </remarks>
public static unsafe class IndirectionCost
{
    private static int s_sink;

    private static T Cast<T>(object obj, Func<T> shape) => (T) obj;

    // the per-shape binder a generator would emit
    private static void AddArgs(DbConnection cnn, object args)
    {
        var typed = Cast(args, static () => new { id = default(int) });
        s_sink += typed.id;
    }

    // created once, at type init; no unsafe needed
    private static readonly Action<DbConnection, object> s_addArgs = AddArgs;

    private static void SharedViaPointer(DbConnection cnn, object args, delegate*<DbConnection, object, void> bind)
        => bind(cnn, args);

    private static void SharedViaDelegate(DbConnection cnn, object args, Action<DbConnection, object> bind)
        => bind(cnn, args);

    private static void SharedInline(DbConnection cnn, object args)
    {
        var typed = Cast(args, static () => new { id = default(int) });
        s_sink += typed.id;
    }

    public static void Run()
    {
        var cnn = new FakeDb();
        const int Warm = 200_000, Iter = 20_000_000;

        // warm up all three
        for (var i = 0; i < Warm; i++) { SharedInline(cnn, new { id = 1 }); SharedViaPointer(cnn, new { id = 1 }, &AddArgs); SharedViaDelegate(cnn, new { id = 1 }, s_addArgs); }

        Console.WriteLine("Allocation per call (the 24 B args object is the caller's, in all three):");
        long b0 = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++) SharedInline(cnn, new { id = 1 });
        long b1 = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++) SharedViaPointer(cnn, new { id = 1 }, &AddArgs);
        long b2 = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++) SharedViaDelegate(cnn, new { id = 1 }, s_addArgs);
        long b3 = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"    inline body                    {(b1 - b0) / 100_000.0,5:0.0} B");
        Console.WriteLine($"    via delegate* parameter        {(b2 - b1) / 100_000.0,5:0.0} B");
        Console.WriteLine($"    via static readonly delegate   {(b3 - b2) / 100_000.0,5:0.0} B");

        Console.WriteLine();
        Console.WriteLine("Cost per call, same work, different indirection:");
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < Iter; i++) SharedInline(cnn, new { id = 1 });
        sw.Stop(); var t0 = sw.Elapsed.TotalNanoseconds / Iter;
        sw.Restart();
        for (var i = 0; i < Iter; i++) SharedViaPointer(cnn, new { id = 1 }, &AddArgs);
        sw.Stop(); var t1 = sw.Elapsed.TotalNanoseconds / Iter;
        sw.Restart();
        for (var i = 0; i < Iter; i++) SharedViaDelegate(cnn, new { id = 1 }, s_addArgs);
        sw.Stop(); var t2 = sw.Elapsed.TotalNanoseconds / Iter;
        Console.WriteLine($"    inline body                    {t0,5:0.00} ns");
        Console.WriteLine($"    via delegate* parameter        {t1,5:0.00} ns  (+{t1 - t0:0.00})");
        Console.WriteLine($"    via static readonly delegate   {t2,5:0.00} ns  (+{t2 - t0:0.00})");
        Console.WriteLine($"    (sink {s_sink})");
    }
}
