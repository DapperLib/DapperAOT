using System;
using System.Data.Common;

try
{
    using DbConnection conn = null!; // new Microsoft.Data.SqlClient.SqlConnection("garbage");
    var customer = Foo.GetCustomer(conn, 42, "north");
    Console.WriteLine($"{customer.X}, {customer.Y}, {customer.Z}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return -1;
}