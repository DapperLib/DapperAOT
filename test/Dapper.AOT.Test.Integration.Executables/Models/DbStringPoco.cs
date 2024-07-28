namespace Dapper.AOT.Test.Integration.Executables.Models;

public class DbStringPoco
{
    public const string TableName = "dbString_test";
    
    public int Id { get; set; }
    public string Name { get; set; }
}