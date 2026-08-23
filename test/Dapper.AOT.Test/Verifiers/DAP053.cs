using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP053 : Verifier<DapperAnalyzer>
{
    // what a PublishAot project looks like to an analyzer
    private static readonly Func<Solution, ProjectId, Solution> PublishAot = (solution, projectId) =>
        solution.AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "PublishAot.globalconfig",
            SourceText.From("is_global = true\r\nbuild_property.PublishAot = true\r\n", Encoding.UTF8),
            filePath: "/PublishAot.globalconfig");

    private const string OptedIn = """
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: UseRuntimeTypeHandlers]

        public static class Foo
        {
            static void SomeCode(DbConnection connection)
                => connection.Execute("insert Events (Name) values (@Name)", new { Name = "abc" });
        }
        """;

    [Fact] // opting in is a JIT-hosted migration mode; under PublishAot it is a trap
    public Task ReportedWhenPublishingAot() => CSVerifyAsync(OptedIn, [.. DefaultConfig, PublishAot], [
        Diagnostic(Diagnostics.RuntimeTypeHandlersUnderAot).WithSpan(5, 10, 5, 32), // the attribute itself
    ]);

    [Fact] // the same code, JIT-hosted: opting in is a supported (if transitional) choice
    public Task NotReportedWithoutPublishAot() => CSVerifyAsync(OptedIn, DefaultConfig, []);

    [Fact] // the default posture, which is what we want people publishing
    public Task NotReportedWhenNotOptedIn() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]

        public static class Foo
        {
            static void SomeCode(DbConnection connection)
                => connection.Execute("insert Events (Name) values (@Name)", new { Name = "abc" });
        }
        """, [.. DefaultConfig, PublishAot], []);

    [Fact] // the explicit opt-out spelling is not an opt-in
    public Task NotReportedWhenExplicitlyDisabled() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: UseRuntimeTypeHandlers(false)]

        public static class Foo
        {
            static void SomeCode(DbConnection connection)
                => connection.Execute("insert Events (Name) values (@Name)", new { Name = "abc" });
        }
        """, [.. DefaultConfig, PublishAot], []);
}
