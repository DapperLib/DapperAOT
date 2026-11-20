using Dapper.CodeAnalysis;
using Dapper.SqlAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP244 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ValidNonAggregate() => SqlVerifyAsync("""
        select Name
        from SomeTable
        """);

    [Fact]
    public Task ValidAggregate() => SqlVerifyAsync("""
        select COUNT(1), COUNT(DISTINCT Name)
        from SomeTable
        """);

    [Fact]
    public Task InvalidMix() => SqlVerifyAsync("""
        {|#0:select COUNT(1), COUNT(DISTINCT Name), Name
        from SomeTable|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact] // false positive https://github.com/DapperLib/DapperAOT/issues/235
    public Task ValidGroupBy() => SqlVerifyAsync("""
        select CustomerId, COUNT(1) as OrderCount, Largest = MAX(Total)
        from Orders
        group by CustomerId
        """);

    [Fact] // false positive https://github.com/DapperLib/DapperAOT/issues/235
    public Task ValidWindowedAggregate() => SqlVerifyAsync("""
        select Id, Name, COUNT(1) over () as TotalRows
        from Customers
        """);

    [Fact]
    public Task ValidWindowedCountStarBesideAggregate() => SqlVerifyAsync("""
        select SUM(Total) as GrandTotal, COUNT(*) over () as GroupCount
        from Orders
        """);

    [Fact]
    public Task ValidPartitionedWindowedAggregate() => SqlVerifyAsync("""
        select Id, CustomerId, SUM(Total) over (partition by CustomerId) as CustomerTotal
        from Orders
        """);

    [Fact] // false positive https://github.com/DapperLib/DapperAOT/issues/235
    public Task ValidWrappedAggregate() => SqlVerifyAsync("""
        select COUNT(1) as OrderCount, ISNULL(SUM(Total), 0) as GrandTotal
        from Orders
        """);

    [Theory]
    [InlineData("APPROX_PERCENTILE_CONT(0.5) within group (order by Total)")]
    [InlineData("APPROX_PERCENTILE_DISC(0.5) within group (order by Total)")]
    [InlineData("JSON_ARRAYAGG(Total)")]
    [InlineData("ANY_VALUE(Total)")]
    [InlineData("APPROX_MEDIAN(Total)")]
    [InlineData("APPROX_QUANTILE(Total, 0.5)")]
    [InlineData("MEDIAN(Total)")]
    [InlineData("PERCENTILE_CONT(0.5) within group (order by Total)")]
    [InlineData("PERCENTILE_DISC(0.5) within group (order by Total)")]
    [InlineData("PRODUCT(Total)")]
    [InlineData("QUANTILE(Total, 0.5)")]
    public Task ValidNewerAggregates(string expression) => SqlVerifyAsync($"""
        select COUNT(1) as OrderCount, {expression} as Value
        from Orders
        """);

    [Fact]
    public Task ValidGroupingSets() => SqlVerifyAsync("""
        select CustomerId, COUNT(1) as OrderCount
        from Orders
        group by grouping sets ((CustomerId), ())
        """);

    [Theory]
    [InlineData("COALESCE(MAX(Total), 0)")]
    [InlineData("CAST(AVG(Total) as decimal(10, 2))")]
    [InlineData("case when COUNT(1) > 0 then 1 else 0 end")]
    [InlineData("case COUNT(1) when 0 then 'none' else 'some' end")]
    [InlineData("DATEADD(day, 1, MAX(OrderDate))")]
    [InlineData("(MIN(Total))")]
    public Task ValidWrappedAggregateForms(string expression) => SqlVerifyAsync($"""
        select COUNT(1) as OrderCount, {expression} as Value
        from Orders
        """);

    [Fact]
    public Task ValidConstantsBesideAggregate() => SqlVerifyAsync("""
        declare @cutoff datetime = DATEADD(day, -30, GETDATE());
        select GETDATE() as AsOf, @cutoff as Cutoff, COUNT(1) as OrderCount
        from Orders
        where OrderDate >= @cutoff
        """);

    [Fact]
    public Task ValidScalarSubqueryColumn() => SqlVerifyAsync("""
        select (select MAX(Total) from Orders) as Largest, COUNT(1) as CustomerCount
        from Customers
        """);

    [Fact]
    public Task ValidNestedScalarSubquery() => SqlVerifyAsync("""
        select COUNT(1) as OrderCount,
            ISNULL((select MAX(o2.Total) from Orders o2 where o2.CustomerId = 1), 0) as Largest
        from Orders
        """);

    [Fact]
    public Task ValidSubqueryAliasShadowsOuter() => SqlVerifyAsync("""
        select COUNT(1) as OrderCount, (select top 1 o.Total from Orders o order by o.Total desc) as Largest
        from Orders o
        """);

    [Fact]
    public Task ValidOuterReferenceInsideSubqueryAggregate() => SqlVerifyAsync("""
        select COUNT(1) as OrderCount, (select SUM(o.Total)) as GrandTotal
        from Orders o
        """);

    [Fact]
    public Task ValidCorrelatedSubqueryWithGroupBy() => SqlVerifyAsync("""
        select o.CustomerId, COUNT(1) as OrderCount,
            (select MAX(c.Name) from Customers c where c.Id = o.CustomerId) as CustomerName
        from Orders o
        group by o.CustomerId
        """);

    [Fact]
    public Task InvalidMixCorrelatedSubquery() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select MAX(p.Amount) from Payments p where p.OrderId = o.Id) as Paid
        from Orders o|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixCorrelatedExists() => SqlVerifyAsync("""
        {|#0:select COUNT(1), case when exists (select 1 from Payments p where p.OrderId = o.Id) then 1 else 0 end as IsPaid
        from Orders o
        inner join Customers c on c.Id = o.CustomerId|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixCorrelatedByTableName() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select MAX(p.Amount) from Payments p where p.OrderId = Orders.Id) as Paid
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixCorrelatedDerivedTable() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select MAX(p.Amount) from Payments p where p.OrderId = x.Id) as Paid
        from (select Id from Orders) x|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixCorrelatedInEachStatement() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select MAX(p.Amount) from Payments p where p.OrderId = o.Id) as Paid
        from Orders o|};
        {|#1:select COUNT(1), (select MAX(p.Amount) from Payments p where p.CustomerId = c.Id) as Paid
        from Customers c|};
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0),
            Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(1));

    [Fact] // dbo.Sum is a user function, so o.Total is not inside an aggregate
    public Task InvalidMixCorrelatedThroughUserFunction() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select dbo.Sum(o.Total)) as Total
        from Orders o|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact] // built-in aggregates are never schema-qualified
    public Task ValidUserFunctionNamedLikeAggregate() => SqlVerifyAsync("""
        select dbo.Product(Price, Quantity) as LineTotal, Name
        from Orders
        """);

    [Fact]
    public Task InvalidMixEmptyGroupBy() => SqlVerifyAsync("""
        {|#0:select Name, COUNT(1)
        from Customers
        group by ()|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact] // with OVER, PERCENTILE_CONT is windowed, so the ungrouped Total still counts
    public Task InvalidMixWindowedPercentile() => SqlVerifyAsync("""
        {|#0:select SUM(Total), PERCENTILE_CONT(0.5) within group (order by Total) over ()
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixEmptyGroupingSets() => SqlVerifyAsync("""
        {|#0:select Name, COUNT(1)
        from Customers
        group by grouping sets (())|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixChecksumStar() => SqlVerifyAsync("""
        {|#0:select COUNT(1), CHECKSUM(*)
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixIdentity() => SqlVerifyAsync("""
        {|#0:select IDENTITY(int, 1, 1) as Id, COUNT(1) as OrderCount
        into #Counts
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixMethodCall() => SqlVerifyAsync("""
        {|#0:select COUNT(1), s.Location.STAsText()
        from Sites s|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixCorrelatedMethodCall() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select top 1 r.Name from Regions r where r.Code = s.Location.STAsText()) as Region
        from Sites s|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact] // in a case-sensitive database the inner O does not hide the outer o
    public Task InvalidMixCorrelatedCaseSensitive() => SqlVerifyAsync("""
        {|#0:select COUNT(1), (select top 1 O.Amount from Payments O where O.OrderId = o.Id) as Paid
        from Orders o|}
        """, SqlParseInputFlags.CaseSensitive, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task ValidCorrelatedCaseInsensitive() => SqlVerifyAsync("""
        select COUNT(1), (select top 1 O.Amount from Payments O where O.OrderId = o.Id) as Paid
        from Orders o
        """);

    [Theory]
    [InlineData("dbo.FormatMoney(SUM(Total))")]
    [InlineData("dbo.CurrentRate()")]
    [InlineData("CAST(MAX(Notes) as xml).value('(/a)[1]', 'int')")]
    [InlineData("NEXT VALUE FOR dbo.OrderNumbers")]
    public Task ValidUserFunctionsMethodsAndSequences(string expression) => SqlVerifyAsync($"""
        select COUNT(1) as OrderCount, {expression} as Value
        from Orders
        """);

    [Fact]
    public Task InvalidMixWithGroupByInSubquery() => SqlVerifyAsync("""
        {|#0:select COUNT(1), Name
        from (select Name from Customers group by Name) x|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixWrappedColumn() => SqlVerifyAsync("""
        {|#0:select COUNT(1), ISNULL(Name, '') as Name
        from Customers|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixColumnInCase() => SqlVerifyAsync("""
        {|#0:select COUNT(1), case when Total > 0 then 1 else 0 end as IsPositive
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixColumnInsideWrappedAggregate() => SqlVerifyAsync("""
        {|#0:select ISNULL(SUM(Total), CustomerId) as Total
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

    [Fact]
    public Task InvalidMixWindowedOverColumn() => SqlVerifyAsync("""
        {|#0:select SUM(Total), COUNT(1) over (partition by CustomerId)
        from Orders|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

}