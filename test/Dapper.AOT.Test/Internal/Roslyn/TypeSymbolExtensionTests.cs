using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Dapper.TestCommon;
using static Dapper.Internal.Inspection;

namespace Dapper.Internal.Roslyn
{
    public class TypeSymbolExtensionsTests
    {
        [Fact]
        public void GetMethods_ProperlyPassesTypeMethods()
        {
            var text = BuildDapperCodeText(
                """
                    _ = connection.Execute("def", new Customer());
                """,
                withFactoryMethod: true
            );

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            var result = typeSymbol.GetMethods()!.ToArray();
            Assert.Single(result);
        }
        
        [Fact]
        public void CheckTypeUsage_WithCustomConstructor()
        {
            var text = BuildDapperCodeText(
                """
                    _ = connection.Execute("def", new Customer());
                """,
                withConstructor: true
            );

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            var result = ChooseConstructor(typeSymbol, out var ctor);
            Assert.Equal(ConstructorResult.SuccessSingleImplicit, result);
            Assert.NotNull(ctor);
        }

        [Fact]
        public void CheckSystemObject()
        {
            var text = BuildDapperCodeText(
            """
            _ = connection.Execute("def", new Customer());
            """);

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            Assert.True(typeSymbol.IsNullable());
        }

        [Fact]
        public void CheckCollectionType_TwoDimensionalArray()
        {
            var text = BuildDapperCodeText(
            """
            var customers = new Customer[4, 2];
            _ = connection.Execute("def", customers);
            """);

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            Assert.False(typeSymbol.IsArray());
        }

        [Fact]
        public void CheckCollectionType_AnonymousObjectsList()
        {
            var text = BuildDapperCodeText(
            """
            var customers = (new[] { new { Foo = 12, bar }, new { Foo = 53, bar = "abc" } }).AsList();
            _ = connection.Execute("def", customers);
            """);

            var argumentOperation = GetInvocationArgumentOperation(text, invocationIndex: 1);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            // implements
            Assert.True(typeSymbol.ImplementsIEnumerable(out _));
            Assert.True(typeSymbol.ImplementsIList(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyList(out _));
            Assert.True(typeSymbol.ImplementsICollection(out _));

            // is
            Assert.True(typeSymbol.IsList());
            Assert.False(typeSymbol.IsArray());
            Assert.False(typeSymbol.IsImmutableArray());

            Assert.Equal("object?", typeSymbol.GetContainingTypeDisplayName());
        }

        [Fact]
        public void CheckCollectionType_Array()
        {
            var text = BuildDapperCodeText(
            """
            var customers = new Customer[] { new Customer(), new Customer() };
            _ = connection.Execute("def", customers);
            """);

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            // implements
            Assert.True(typeSymbol.ImplementsIEnumerable(out _));
            Assert.True(typeSymbol.ImplementsIList(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyList(out _));
            Assert.True(typeSymbol.ImplementsICollection(out _));

            // is
            Assert.True(typeSymbol.IsArray());
            Assert.False(typeSymbol.IsList());
            Assert.False(typeSymbol.IsImmutableArray());

            Assert.Equal("global::Foo.Customer", typeSymbol.GetContainingTypeDisplayName());
        }

        [Fact]
        public void CheckEnum()
        {
            var text = BuildDapperCodeText(
            """
            _ = connection.Execute("def", State.Active);
            """);

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            Assert.Equal("int", typeSymbol.GetUnderlyingEnumTypeName());
            Assert.Equal("global::Foo.State", typeSymbol.GetTypeDisplayName());
        }

        [Fact]
        public void CheckCollectionType_GenericList()
        {
            var text = BuildDapperCodeText(
            """
            var customers = new List<Customer>() { new Customer(), new Customer() };
            _ = connection.Execute("def", customers);
            """);

            var argumentOperation = GetInvocationArgumentOperation(text);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            // implements
            Assert.True(typeSymbol.ImplementsIEnumerable(out _));
            Assert.True(typeSymbol.ImplementsIList(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyList(out _));
            Assert.True(typeSymbol.ImplementsICollection(out _));

            // is
            Assert.True(typeSymbol.IsList());
            Assert.False(typeSymbol.IsArray());
            Assert.False(typeSymbol.IsImmutableArray());

            Assert.Equal("global::Foo.Customer", typeSymbol.GetContainingTypeDisplayName());
        }

        [Fact]
        public void CheckCollectionType_ImmutableArray()
        {
            var text = BuildDapperCodeText(
            """
            var customers = ImmutableArray.Create<Customer>(new Customer(), new Customer());
            _ = connection.Execute("def", customers);
            """);

            var argumentOperation = GetInvocationArgumentOperation(text, invocationIndex: 1);
            var typeSymbol = GetConversionTypeSymbol(argumentOperation);

            // assert
            Assert.True(typeSymbol.ImplementsIEnumerable(out _));
            Assert.True(typeSymbol.ImplementsIList(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection(out _));
            Assert.True(typeSymbol.ImplementsIReadOnlyList(out _));
            Assert.True(typeSymbol.ImplementsICollection(out _));

            // is
            Assert.True(typeSymbol.IsImmutableArray());
            Assert.False(typeSymbol.IsList());
            Assert.False(typeSymbol.IsArray());

            Assert.Equal("global::Foo.Customer", typeSymbol.GetContainingTypeDisplayName());
        }

        static ITypeSymbol GetConversionTypeSymbol(IArgumentOperation argumentOperation)
        {
            var conv = argumentOperation.Value as IConversionOperation;
            Assert.NotNull(conv);
            var typeSymbol = conv!.Operand.Type;
            Assert.NotNull(typeSymbol);
            return typeSymbol!;
        }

        static IArgumentOperation GetInvocationArgumentOperation(string text, int invocationIndex = 0, int argumentIndex = 2)
        {
            var compilation = RoslynTestHelpers.CreateCompilation(text, "assembly", "file");
            var tree = compilation.SyntaxTrees.Single();
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            var invocationNode = nodes[invocationIndex];
            var invocationOperation = semanticModel.GetOperation(invocationNode) as IInvocationOperation;
            Assert.NotNull(invocationOperation);

            var arg = invocationOperation!.Arguments[argumentIndex];
            Assert.NotNull(arg);
            return arg;
        }

        private const string ConstructorSnippet = 
        """
            public Customer(int x, string y, double? z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        """;
        private const string FactoryMethodSnippet =
        """
            public static Customer Create(int x, string y, double? z)
            {
                return new Customer { X = x, Y = y, Z = z };
            }
        """;

        static string BuildDapperCodeText(
            string implementation, 
            bool withConstructor = false,
            bool withFactoryMethod = false) 
        => $$"""
            using Dapper;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Data;
            using System.Data.Common;

            public static class Foo
            {
                static void SomeCode(DbConnection connection, string bar)
                {
                    {{implementation}}
                }
                public class Customer
                {
                    public int X { get; set; }
                    public string Y;
                    public double? Z { get; set; }

                    {{(withConstructor ? ConstructorSnippet : string.Empty)}}
                    {{(withFactoryMethod ? FactoryMethodSnippet : string.Empty)}}
                }
                public enum State
                {
                    Active,
                    Disabled
                }
            }
        """;
    }
}