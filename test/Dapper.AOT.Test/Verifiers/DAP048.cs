using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP048 : Verifier<DapperAnalyzer>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task MoveFromDbString(bool dapperAotEnabled) => CSVerifyAsync($$"""
              using Dapper;
              using System;
              using System.Linq;
              using System.Data.Common;
              using System.Threading;
              using System.Threading.Tasks;

              [DapperAot({{dapperAotEnabled.ToString().ToLower()}})]
              class WithoutAot
              {
                  public void DapperCode(DbConnection conn)
                  {
                      var sql = "SELECT * FROM Cars WHERE Name = @Name;";
                      var cars = conn.Query<Car>(sql,
                          new
                          {
                              {|#0:Name|} = new DbString
                              {
                                  Value = "MyCar",
                                  IsFixedLength = false,
                                  Length = 5,
                                  IsAnsi = true
                              }
                          });
                  }
              }

              public class Car
              {
                  public string Name { get; set; }
                  public int Speed { get; set; }
              }
              """,
        DefaultConfig,
        dapperAotEnabled ? [ Diagnostic(Diagnostics.MoveFromDbString).WithLocation(0) ] : []
    );
}