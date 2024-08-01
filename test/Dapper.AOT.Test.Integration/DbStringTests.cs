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
        
        var userCode = Parse("UserCode.cs", $$"""
                                              namespace ProgramNamespace
                                              {
                                                  using System;
                                                  using System.Linq;
                                                  using System.Data;
                                                  using Dapper;
                                                  using Dapper.AOT.Test.Integration.Executables.Models;
                                              
                                                  public static class Program
                                                  {
                                                      public static DbStringPoco RunCode(IDbConnection dbConnection)
                                                      {
                                                          var results = dbConnection.Query<DbStringPoco>("select * from table");
                                                          return results.First();
                                                      }
                                                  }
                                              }
                                              """);

        var generatedCode = Parse("AnotherFile.cs", $$"""
            namespace Dapper.AOT
            {
              file static class D
              {
                  [System.Runtime.CompilerServices.InterceptsLocation(path: "UserCode.cs", lineNumber: 12, columnNumber: 34)]
                  internal static global::System.Collections.Generic.IEnumerable<global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco> Query0(
                        this global::System.Data.IDbConnection cnn,
                        string sql,
                        object? param = null,
                        global::System.Data.IDbTransaction? transaction = null, bool buffered = true, int? commandTimeout = null, global::System.Data.CommandType? commandType = null)
                  {
                      return new global::System.Collections.Generic.List<global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco>()  
                      {
                          new global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco() { Id = 42, Name = "something" }
                      };
                  }
              }  
              
            }
        """);

        var interceptorsLocationAttributeCode = Parse("InterceptorsLocationAttribute.cs", $$"""
                  namespace System.Runtime.CompilerServices
                  {
                      [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                      public sealed class InterceptsLocationAttribute(string path, int lineNumber, int columnNumber) : Attribute
                      {
                      }
                  }                                        
              """);

        var result = ExecuteInterceptedUserCode<DbStringUsage, DbStringPoco>(dbConnection);
        
        Assert.True(result.Id.Equals(42));
        Assert.True(result.Name.Equals("something", StringComparison.InvariantCultureIgnoreCase));
    }
}