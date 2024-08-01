using System;
using System.Data;
using Dapper.AOT.Test.Integration.Executables.Models;
using Dapper.AOT.Test.Integration.Executables.UserCode;
using Dapper.AOT.Test.Integration.Setup;
using Microsoft.Data.SqlClient;

namespace Dapper.AOT.Test.Integration;

public class DbStringTests : IntegrationTestsBase
{
    [Fact]
    public void Test1()
    {
        string connection = "data source=DMKOR_PC;initial catalog=dapper-experiments;trusted_connection=true;TrustServerCertificate=True";
        IDbConnection dbConnection = new SqlConnection(connection);

        var result = ExecuteInterceptedUserCode<DbStringUsage, DbStringPoco>(dbConnection);
        
        Assert.True(result.ProductId.Equals(1));
        Assert.True(result.Name.Equals("MyProduct", StringComparison.InvariantCultureIgnoreCase));
    }
}