using Dapper;
using System;
using System.Data.Common;

#nullable enable

namespace UsageLinker;

// investigating problem with UsageLinker thinking it is using factory methods

[DapperAot(true)]
public class Product
{
    public static dynamic GetProductDynamic(DbConnection connection, int productId) => connection.QueryFirst(
        "select * from Production.Product where ProductId=@productId", new { productId });

    public static Product GetProduct(DbConnection connection, int productId) => connection.QueryFirst<Product>(
        "select * from Production.Product where ProductId=@productId", new { productId });

    public int ProductID { get; set; }
#pragma warning disable CS8618 // non-null init
    public string Name { get; set; }
    public string ProductNumber { get; set; }
#pragma warning restore CS8618 // non-null init
    public bool MakeFlag { get; set; }
    public bool FinishedGoodsFlag { get; set; }
    public string? Color { get; set; }
    public short SafetyStockLevel { get; set; }
    public short ReorderPoint { get; set; }
    public decimal StandardCost { get; set; }
    public decimal ListPrice { get; set; }
    public string? Size { get; set; }
    public string? SizeUnitMeasureCode { get; set; }
    public string? WeightUnitMeasureCode { get; set; }
    public decimal? Weight { get; set; }
    public int DaysToManufacture { get; set; }
    public string? ProductLine { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
    public int? ProductSubcategoryID { get; set; }
    public int? ProductModelID { get; set; }
    public DateTime SellStartDate { get; set; }
    public DateTime? SellEndDate { get; set; }
    public DateTime? DiscontinuedDate { get; set; }
    public Guid RowGuid { get; set; }
    public DateTime ModifiedDate { get; set; }
}


