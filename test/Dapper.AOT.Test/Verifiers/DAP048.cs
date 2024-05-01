using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP048 : Verifier<DapperAnalyzer>
{
    [Theory]
    [InlineData("[DapperAot(false)]")]
    [InlineData("[DapperAot(true)]")]
    public Task MoveFromDbString(string dapperAotAttribute) => CSVerifyAsync($$"""
        using Dapper;
        using System;
        using System.Linq;
        using System.Data.Common;
        using System.Threading;
        using System.Threading.Tasks;

        {{dapperAotAttribute}}
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
        [
            Diagnostic(Diagnostics.MoveFromDbString)
                .WithLocation(0)
        ]
    );
}