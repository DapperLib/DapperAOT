using System;
using System.Runtime.CompilerServices;

// Does the anonymous args object actually stop allocating when it stays generic and does not
// escape? That is the hypothesis behind wanting a TArgs overload at all, so it is worth testing
// rather than assuming.
public static class Escape
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Generic<TArgs>(TArgs args, Func<TArgs, int> read) => read(args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Erased(object args) => ((dynamic) args).id;

    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("Does a non-escaping generic args object get stack-allocated?");

        // generic, inlineable, field read only -- the shape a TArgs interceptor would have
        Sum(static () => { var a = new { id = 42 }; return a.id; }, "inlined, never crosses a boundary");

        // the same object handed to something typed as object, as today's interceptor does
        Sum(static () => { var a = new { id = 42 }; return Keep(a); }, "passed as object (today's shape)");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Keep(object o) => o.GetHashCode() & 0;

    private static void Sum(Func<int> body, string what)
    {
        for (var i = 0; i < 200; i++) _ = body();   // warm up and tier up
        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = 0;
        for (var i = 0; i < 10_000; i++) total += body();
        var after = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"    {what,-38} {(after - before) / 10_000.0,5:0.0} B per iteration (sum {total})");
    }
}
