using System;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
[DapperAot(false)]
internal class DynamicParametersTest
{
    private SqlConnection connection;

    static readonly DateTime From = new DateTime(2023, 6, 1), To = new DateTime(2023, 6, 30);
    public void UseTrivialDynamicParameters()
    {
        var parameters = new DynamicParameters();
        parameters.Add("@StartDate", From);
        parameters.Add("@EndDate", To);

        connection.Execute("insert Reservations ([Start], [End]) values (@StartDate, @EndDate)", parameters);
        connection.Execute("insert Reservations ([Start], [End]) values (@StartDate, @EndDate)", (SqlMapper.IDynamicParameters)parameters);
        connection.Execute("insert Reservations ([Start], [End]) values (@StartDate, @EndDate)", new Foo());
    }
    public void UseAnonType()
    {
        var parameters = new
        {
            StartDate = From,
            EndDate = To,
        };

        connection.Execute("insert Reservations ([Start], [End]) values (@StartDate, @EndDate)", parameters);
    }
    class Foo : SqlMapper.IDynamicParameters
    {
        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity) { }
    }
}
