using System;
try
{
    await Foo.SomeCode();
    //         using var conn = new SqlConnection("gibberish");
    //var c = Customer.GetCustomer(conn, 42, "South");
    //Console.WriteLine(c.Name);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return -1;
}