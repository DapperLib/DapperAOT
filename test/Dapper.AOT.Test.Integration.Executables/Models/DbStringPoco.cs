namespace Dapper.AOT.Test.Integration.Executables.Models;

public class DbStringPoco
{
    public const string TableName = "Product";
    
    public int ProductId { get; set; }
    public string Name { get; set; }
}