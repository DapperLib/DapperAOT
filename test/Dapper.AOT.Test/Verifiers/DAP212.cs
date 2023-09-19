using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP212 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ExecComposedSql() => SqlVerifyAsync("""
        declare @s nvarchar(100) = 'fred' -- assume this is a parameter
        declare @sql nvarchar(2000) = 'select * from Customers where Name = ''' + @s + '''';
        {|#0:exec (@sql)|};

        {|#1:exec ('select * from Customers where Name = ''' + @s + '''')|};

        exec sp_executesql N'select * from Customers where Name = @name', N'@name nvarchar(100)', @s

        exec ('select * from Customers where Name = ''fred''') -- not composed
        """, Diagnostic(Diagnostics.ExecComposedSql).WithLocation(0),
            Diagnostic(Diagnostics.ExecComposedSql).WithLocation(1));
    
}