# Bulk Copy

A common scenario with databases is "bulk copy"; in the case of SQL Server, this is exposed via
[`SqlBulkCopy.WriteToServer`](https://learn.microsoft.com/dotnet/api/system.data.sqlclient.sqlbulkcopy.writetoserver),
which allows a data feed to be pumped into a table very efficiently and rapidly.

The problem is: that data feed needs to be a `DataTable`, a `DbDataReader`, or similar; but your
application code is likely to be using *objects*, for example a `List<Customer>` or a `IEnumerable<Order>` sequence.

What we need is a mechanism to turn `IEnumerable<T>` (or `IAsyncEnumerable<T>`) into a `DbDataReader`,
suitable for use with `SqlBulkCopy`!

This *is not* a feature that vanilla `Dapper` provides; a library from my distant past,
[`FastMember`](https://www.nuget.org/packages/FastMember), can help bridge this gap, but like `Dapper` it is
based on runtime reflection, and is not AOT compatible.

## Introducing `TypeAccessor`

A new API in `Dapper.AOT` gives us what we need - welcome `TypeAccessor`! Usage is simple:

``` csharp
IEnumerable<Customer> customers = ...
var reader = TypeAccessor.CreateDataReader(customers);
```

This gives us a `DbDataReader` that iterates over the provided sequence. We can optionally
specify a subset of instance properties/fields to expose (and their order) - and we can start
from either `IEnumerable<T>` or `IAsyncEnumerable<T>`.

To use this with `SqlBulkCopy`, we probably want to specify columns explicitly:

``` csharp
using var table = new SqlBulkCopy(connection)
{
    DestinationTableName = "Customers",
    ColumnMappings =
    {
        { nameof(Customer.CustomerNumber), nameof(Customer.CustomerNumber) }
        { nameof(Customer.Name), nameof(Customer.Name) }
    }
};
table.EnableStreaming = true;
table.WriteToServer(TypeAccessor.CreateDataReader(customers,
    [nameof(Customer.Name), nameof(Customer.CustomerNumber)]));
return table.RowsCopied;
```

This writes the data from our `customers` sequence into the `Customers` table. To be explicit,
we've specified the column mappings manually, although you can often omit this. Likewise, we have
restricted the `DbDataReader` to only expose the `Name` and `CustomerNumber` members.

This all works using generated AOT-compatible code, and does not require any additional
libaries other than `Dapper.AOT`.


