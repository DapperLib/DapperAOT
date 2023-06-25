using System;
using System.Collections.Generic;
using System.Diagnostics;
using UsageLinker;

try
{
    using var conn = new Microsoft.Data.SqlClient.SqlConnection("Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True");
    conn.Open();
    var watch = Stopwatch.StartNew();
    var obj = Product.GetProduct(conn, 752);
    watch.Stop();
    Console.WriteLine($"First time: {watch.ElapsedMilliseconds}ms");
    Console.WriteLine($"Typed object: {obj.ProductID}: {obj.Name} ({obj.ProductNumber})");

    var dobj = Product.GetProductDynamic(conn, 751);
    Console.WriteLine("Got dynamic object; trying to access...");
    try
    {
        Console.WriteLine($"Success: {(int)dobj.ProductID}: {(string)dobj.Name} ({(string)dobj.ProductNumber})");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed: " + ex.Message);
    }
    if (dobj is IDictionary<string,object> robj)
    {
        Console.WriteLine("Got record object; trying to access...");
        try
        {
            Console.WriteLine($"Success: {robj["ProductId"]}: {robj["Name"]} ({robj["ProductNumber"]})");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed: " + ex.Message);
        }

    }

    watch.Restart();
    const int LOOP = 2000;
    for (int i = 0; i < LOOP; i++)
    {
        obj = Product.GetProduct(conn, 752);
    }
    watch.Stop();
    Console.WriteLine($"Next {LOOP} times: {watch.ElapsedMilliseconds}ms");
    Console.WriteLine($"{obj.ProductID}: {obj.Name} ({obj.ProductNumber})");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex.StackTrace);
    return -1;
}