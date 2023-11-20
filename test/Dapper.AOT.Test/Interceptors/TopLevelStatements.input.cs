using Dapper;
using System;
using System.Data.Common;
using System.Threading.Tasks;


[module: DapperAot]

#if !NETFRAMEWORK
Console.WriteLine(System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported ? "Running with JIT" : "Running with AOT");
#endif

// Dapper.AOT, at least at the moment (it was released like 4 days ago), doesn't seem to be able to handle things if we don't put it in a class
app.MapGet("/", async (HttpContext context, SqlConnectionFactory sqlConnectionFactory, Stack stack) =>
{
#if !NETFRAMEWORK
    await
#endif
    using var connection = sqlConnectionFactory();
    var item = await connection.QuerySingleAsync<SomeThing>("SELECT SomeId, SomeText FROM SomeThing LIMIT 1");
    //context.Response.Headers["stack"] = stack.Name;
    return Results.Ok(item);
});

public static class Handlers
{
    public static async Task<IResult> Root(HttpContext context, SqlConnectionFactory sqlConnectionFactory, Stack stack)
    {
#if !NETFRAMEWORK
        await
#endif
        using var connection = sqlConnectionFactory();
        var item = await connection.QuerySingleAsync<SomeThing>("SELECT SomeId, SomeText FROM SomeThing LIMIT 1");
        // context.Response.Headers["stack"] = stack.Name;
        return Results.Ok(item);
    }
}

public record SomeThing(int SomeId, string SomeText);

public record Stack(string Name);


// spoof enough fake http code to build
public static class app
{
    public static void MapGet(string path, Delegate handler) => throw new NotImplementedException();
}

public delegate DbConnection SqlConnectionFactory();
public interface IResult { }
public static class Results
{
    public static IResult Ok(object whatever) => throw new NotImplementedException();
}
public class HttpContext {}

#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    file static class IsExternalInit
    {
    }
}
#endif