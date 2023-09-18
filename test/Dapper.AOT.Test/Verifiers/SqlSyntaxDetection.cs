﻿using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class SqlSyntaxDetection : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task GlobalEnabled_NoAttributes() => CSVerifyAsync("""
        using Dapper;

        class SomeCode
        {
            public void Foo(System.Data.Common.DbConnection conn)
            {
                conn.Execute("{|#0:|}111 invalid");
            }
            public void Foo(System.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#1:|}222 invalid");
            }
            public void Foo(Microsoft.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#2:|}333 invalid");
            }
        }
        """, DefaultConfig, [
            Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
            Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
            Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
        ], assumeSqlServer: true);

    [Fact]
    public Task GlobalDisabled_NoAttributes() => CSVerifyAsync("""
        using Dapper;

        class SomeCode
        {
            public void Foo(System.Data.Common.DbConnection conn)
            {
                conn.Execute("{|#0:|}111 invalid");
            }
            public void Foo(System.Data.SqlClient.SqlConnection conn) // still picked up by type
            {
                conn.Execute("{|#1:|}222 invalid");
            }
            public void Foo(Microsoft.Data.SqlClient.SqlConnection conn) // still picked up by type
            {
                conn.Execute("{|#2:|}333 invalid");
            }
        }
        """, DefaultConfig, [
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
    ], assumeSqlServer: false);

    [Fact]
    public Task GlobalDisabled_MethodAttributes() => CSVerifyAsync("""
        using Dapper;

        class SomeCode
        {
            [SqlSyntax(SqlSyntax.SqlServer)]
            public void Foo(System.Data.Common.DbConnection conn)
            {
                conn.Execute("{|#0:|}111 invalid");
            }
            [SqlSyntax(SqlSyntax.SqlServer)]
            public void Foo(System.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#1:|}222 invalid");
            }
            [SqlSyntax(SqlSyntax.SqlServer)]
            public void Foo(Microsoft.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#2:|}333 invalid");
            }
        }
        """, DefaultConfig, [
            Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
            Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
            Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
        ], assumeSqlServer: false);

    [Fact]
    public Task GlobalDisabled_TypeAttribute() => CSVerifyAsync("""
        using Dapper;

        [SqlSyntax(SqlSyntax.SqlServer)]
        class SomeCode
        {
            public void Foo(System.Data.Common.DbConnection conn)
            {
                conn.Execute("{|#0:|}111 invalid");
            }
            public void Foo(System.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#1:|}222 invalid");
            }
            public void Foo(Microsoft.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#2:|}333 invalid");
            }
        }
        """, DefaultConfig, [
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
    ], assumeSqlServer: false);

    [Fact]
    public Task GlobalDisabled_ContainingTypeAttribute() => CSVerifyAsync("""
        using Dapper;

        [SqlSyntax(SqlSyntax.SqlServer)]
        class SomeWrapper
        {
            class SomeCode
            {
                public void Foo(System.Data.Common.DbConnection conn)
                {
                    conn.Execute("{|#0:|}111 invalid");
                }
                public void Foo(System.Data.SqlClient.SqlConnection conn)
                {
                    conn.Execute("{|#1:|}222 invalid");
                }
                public void Foo(Microsoft.Data.SqlClient.SqlConnection conn)
                {
                    conn.Execute("{|#2:|}333 invalid");
                }
            }
        }
        """, DefaultConfig, [
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
        .WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
    ], assumeSqlServer: false);

    [Fact]
    public Task GlobalDisabled_ModuleAttribute() => CSVerifyAsync("""
        using Dapper;

        [module:SqlSyntax(SqlSyntax.SqlServer)]

        class SomeCode
        {
            public void Foo(System.Data.Common.DbConnection conn)
            {
                conn.Execute("{|#0:|}111 invalid");
            }
            public void Foo(System.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#1:|}222 invalid");
            }
            public void Foo(Microsoft.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#2:|}333 invalid");
            }
        }
        """, DefaultConfig, [
    Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
    Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
    Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
], assumeSqlServer: false);

    [Fact]
    public Task GlobalDisabled_AssemblyAttribute() => CSVerifyAsync("""
        using Dapper;

        [assembly:SqlSyntax(SqlSyntax.SqlServer)]

        class SomeCode
        {
            public void Foo(System.Data.Common.DbConnection conn)
            {
                conn.Execute("{|#0:|}111 invalid");
            }
            public void Foo(System.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#1:|}222 invalid");
            }
            public void Foo(Microsoft.Data.SqlClient.SqlConnection conn)
            {
                conn.Execute("{|#2:|}333 invalid");
            }
        }
        """, DefaultConfig, [
Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(1).WithArguments(46010, "Incorrect syntax near 222."),
Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(2).WithArguments(46010, "Incorrect syntax near 333."),
], assumeSqlServer: false);


}