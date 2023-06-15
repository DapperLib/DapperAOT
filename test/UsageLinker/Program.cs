using System;
using System.Data.Common;
using System.Diagnostics;
using UsageLinker;

try
{
    using DbConnection conn = new Microsoft.Data.SqlClient.SqlConnection("Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True");
    conn.Open();
    var watch = Stopwatch.StartNew();
    var obj = Product.GetProduct(conn, 752);
    watch.Stop();
    Console.WriteLine($"First time: {watch.ElapsedMilliseconds}ms");
    Console.WriteLine($"{obj.ProductID}: {obj.Name} ({obj.ProductNumber})");

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
    return -1;
}