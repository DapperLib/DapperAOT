using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis.Model;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Dapper.AOT.Test;

/// <summary>
/// The [CommandProperty] member probe: a public mutable field is assignable, a readonly
/// field is not (this was inverted for a long time - it never fired in anger because real
/// ADO.NET command types expose these knobs as properties).
/// </summary>
public class CommandPropertyMemberTests
{
    const string Source = """
        public class SomeCommandType
        {
            public int MutableField;
            public readonly int ReadOnlyField;
            public int SettableProperty { get; set; }
            public int GetOnlyProperty { get; }
            public static int StaticProperty { get; set; }
        }
        """;

    static bool MemberExists(string name)
    {
        var compilation = RoslynTestHelpers.CreateCompilation(Source, "cmdprop_test", "Input.cs");
        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName("SomeCommandType").Single();
        return CommandProperty.Create(type, name, 42, null).MemberExists;
    }

    [Theory]
    [InlineData("MutableField", true)]
    [InlineData("ReadOnlyField", false)]
    [InlineData("SettableProperty", true)]
    [InlineData("GetOnlyProperty", false)]
    [InlineData("StaticProperty", false)]
    [InlineData("DoesNotExist", false)]
    public void MemberProbeReflectsAssignability(string name, bool expected)
        => Assert.Equal(expected, MemberExists(name));
}
