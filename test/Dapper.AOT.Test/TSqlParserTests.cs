﻿using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using Xunit;
using Xunit.Abstractions;
using static Dapper.SqlAnalysis.TSqlProcessor;

namespace Dapper.AOT.Test;

public class TSqlParserTests
{
    public TSqlParserTests(ITestOutputHelper log)
        => this._log = log.WriteLine;

    private readonly Action<string> _log;

    private TestTSqlProcessor GetProcessor() => new TestTSqlProcessor(log: _log);

    [Fact]
    public void DetectBadNullLiteralUsage()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int = null;
            select *
            from Customers
            where Id = null and Name != null;

            select @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Empty(args);
        Assert.Equal(2, errors.Length);
        Assert.Equal("Null literals should not be used in binary comparisons; prefer 'is null' and 'is not null' L4 C12", errors[0]);
        Assert.Equal("Null literals should not be used in binary comparisons; prefer 'is null' and 'is not null' L4 C29", errors[1]);
    }

    [Fact]
    public void DetectExecParams()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @a int, @c int;
            print @b;
            exec @c = SomeProc @x = @a out, @y = @a, @z = @b out; -- middle a is not assigned, b is param
            select @a, @b, @c -- all fine
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@b", Assert.Single(args));
        Assert.Equal(new[] {
            "Variable @a accessed before being assigned L3 C38",
            }, errors);
    }

    [Fact]
    public void ParseValidNullUsage()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int = null;
            select *
            from Customers
            where Id is null and Name is not null
            select @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Empty(args);
        Assert.Empty(errors);
    }

    [Fact]
    public void WarnOfIdentityUsage()
    {
        var parser = GetProcessor();
        parser.Execute("""
            insert customers (id) values (42);
            select @@identity;
            select SCOPE_IDENTITY();
            declare @id int = SCOPE_IDENTITY(); -- no warn
            select @id
            set @id = @@identity; -- warn
            select @id
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Empty(args);
        Assert.Equal(3, errors.Length);
        Assert.Equal("@@identity should not be used; use SCOPE_IDENTITY() instead L2 C8", errors[0]);
        Assert.Equal("Consider OUTPUT INSERTED.yourid on the INSERT instead of SELECT SCOPE_IDENTITY() L3 C8", errors[1]);
        Assert.Equal("@@identity should not be used; use SCOPE_IDENTITY() instead L6 C11", errors[2]);
    }

    [Fact]
    public void ParseValidIdentityUsage()
    {
        var parser = GetProcessor();
        parser.Execute("""
            insert customers (Name)
            output INSERTED.Id
            values ('Name');
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Empty(args);
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseValidInlineDeclarationAndAssignment()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int = 42;
            select *
            from Customers
            where Id = @id and S = @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseValidSeparateDeclarationAndAssignment()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int
            set @s = 42

            select *
            from Customers
            where Id = @id and S = @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseValidExecOutput()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int
            exec someproc @s output

            select *
            from Customers
            where Id = @id and S = @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseValidExecAssignResult()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int
            exec @s = someproc

            select *
            from Customers
            where Id = @id and S = @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseValidSelectInto()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s table (id int not null)

            insert customers (name)
            output INSERTED.id into @s (id)
            values('Name')

            select *
            from Customers
            where Id in (select id from @s)
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Empty(args);
        Assert.Empty(errors);
    }

    [Fact]
    public void ReportNotAssignedBeforeExec()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int
            exec someproc @s

            set @s = 42;
            select *
            from Customers
            where Id = @id and S = @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Equal("Variable @s accessed before being assigned L2 C15", Assert.Single(errors));
    }

    [Fact]
    public void ParseTableVariableNotPopulated()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s table (id int not null)
            
            select *
            from Customers
            where Foo in (select id from @s) and Id=@id
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Equal("Variable @s accessed before being populated L5 C30", Assert.Single(errors));
    }

    [Fact]
    public void ParseValidTableVariablePopulated()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s table (id int not null)
            insert @s (id) values (42);
            select *
            from Customers
            where Foo in (select id from @s) and Id=@id
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseValidSeparateDeclarationAndSelectAssignment()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int
            select @s = 42

            select *
            from Customers
            where Id = @id and S = @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Empty(errors);
    }

    [Fact]
    public void ReportVariableAccessedBeforeDeclaration()
    {
        var parser = GetProcessor();
        parser.Execute("""
            select *
            from Customers
            where Id = @id and S = @s

            declare @s int = 42;
            select @s
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Equal("Variable @s accessed before declaration L3 C24", Assert.Single(errors));
    }

    [Fact]
    public void ReportInvalidSelect()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @s int = 42;

            select *
            from Customers
            where Id = @id and S = @s garbage;
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Equal("Incorrect syntax near garbage. (#46010) L5 C27", Assert.Single(errors));
    }

    [Fact]
    public void ReportBadExecUsage()
    {
        var parser = GetProcessor();
        parser.Execute("""
            declare @sql varchar(8000) = concat('select top 5 * from Sales.Store where BusinessEntityId=',@id)
            exec (@sql) -- bad
            go
            exec ('select top 5 * from Sales.Store where BusinessEntityId=296') -- inadvisable, but...
            exec sp_executesql N'select top 5 * from Sales.Store where BusinessEntityId=@bid', N'@bid int', @id -- good
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@id", Assert.Single(args));
        Assert.Equal(2, errors.Length);
        Assert.Equal("EXEC with composed SQL may be susceptible to SQL injection; consider EXEC sp_executesql with parameters L2 C1", errors[0]);
        Assert.Equal("Multiple batches are not permitted L4 C1", errors[1]);
    }

    [Fact]
    public void ConditionalBail()
    {
        var parser = GetProcessor();
        parser.Execute("""
            if @p > 1
            begin
                label:
                while @p > 0
                begin
                    set @p = @p - 1 -- this @p looks unconsumed
                end
            end
            else
            begin
                goto label
            end
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal("@p", Assert.Single(args));
        Assert.Empty(errors); // because we just gave up
    }

    [Theory]
    [InlineData(ParameterDirection.Input)]
    [InlineData(ParameterDirection.Output)]
    [InlineData(ParameterDirection.InputOutput)]

    public void HandleParameterAssignment(ParameterDirection direction)
    {
        var parser = GetProcessor();
        parser.AddParameter("@a", direction); // consumed before assign
        parser.AddParameter("@b", direction); // assigned, consumed
        parser.AddParameter("@c", direction); // assigned, not consumed
        parser.Execute("""
            select @a;

            set @b = 42;
            print @b

            set @c = 42;
            """);

        var args = parser.GetParameters(out var errors);
        Assert.Equal(new[] { "@a", "@b", "@c" }, args);
        switch (direction)
        {
            case ParameterDirection.Input:
                Assert.Equal(new[]
                {
                    "Variable @b has a value that is not consumed L3 C5",
                    "Variable @c has a value that is not consumed L6 C5", // once for the incoming
                    "Variable @c has a value that is not consumed L6 C5", // once for the outgoing
                }, errors);
                break;
            case ParameterDirection.Output:
                Assert.Equal(new[]
                {
                    "Variable @a accessed before being assigned L1 C8",
                }, errors);
                break;
            case ParameterDirection.InputOutput:
                Assert.Equal(new[]
                {
                    "Variable @b has a value that is not consumed L3 C5",
                    "Variable @c has a value that is not consumed L6 C5",
                }, errors);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }

    class TestTSqlProcessor : TSqlProcessor
    {
        public TestTSqlProcessor(ITypeSymbol? parameterType = null, bool caseSensitive = false, Action<string>? log = null) : base(caseSensitive, log) { }
        public string[] GetParameters(out string[] errors)
        {
            var parameters = (from p in this.Variables
                              where p.IsParameter
                              orderby p.Name
                              select p.Name).ToArray();
            errors = this.errors.ToArray();
            return parameters;
        }

        private Dictionary<string, ParameterDirection>? _extraArgs;

        public void AddParameter(string name, ParameterDirection direction) => (_extraArgs ??= new()).Add(name, direction);

        private readonly List<string> errors = new();
        protected override void OnError(string error, in Location location)
        {
            errors.Add($"{error} {location}");
        }

        protected override bool TryGetParameter(string name, out ParameterDirection direction)
        {
            if (_extraArgs is not null && _extraArgs.TryGetValue(name, out direction)) return true;
            return base.TryGetParameter(name, out direction);
        }
    }
}
