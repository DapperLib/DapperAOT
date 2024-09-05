namespace Dapper.AOT.Test.Integration.Executables.Models;

public class DbStringPoco
{
    public const string TableName = "dbStringPoco";
    
    public int Id { get; set; }
    public string? Name { get; set; }
}