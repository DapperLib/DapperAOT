﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Dapper.TestCommon;

namespace Dapper.Internal.Roslyn
{
    public class TypeSymbolExtensionsTests
    {
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
            Assert.True(typeSymbol.ImplementsIEnumerable());
            Assert.True(typeSymbol.ImplementsGenericIEnumerable());
            Assert.True(typeSymbol.ImplementsIList());
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection());
            Assert.True(typeSymbol.ImplementsIReadOnlyList());
            Assert.True(typeSymbol.ImplementsICollection());

            // is
            Assert.True(typeSymbol.IsList());
            Assert.False(typeSymbol.IsArray());
            Assert.False(typeSymbol.IsImmutableArray());
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
            Assert.True(typeSymbol.ImplementsIEnumerable());
            Assert.True(typeSymbol.ImplementsGenericIEnumerable());
            Assert.True(typeSymbol.ImplementsIList());
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection());
            Assert.True(typeSymbol.ImplementsIReadOnlyList());
            Assert.True(typeSymbol.ImplementsICollection());

            // is
            Assert.True(typeSymbol.IsArray());
            Assert.False(typeSymbol.IsList());
            Assert.False(typeSymbol.IsImmutableArray());

            Assert.Equal("Foo.Customer", typeSymbol.GetContainingTypeFullName());
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
            Assert.True(typeSymbol.ImplementsIEnumerable());
            Assert.True(typeSymbol.ImplementsGenericIEnumerable());
            Assert.True(typeSymbol.ImplementsIList());
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection());
            Assert.True(typeSymbol.ImplementsIReadOnlyList());
            Assert.True(typeSymbol.ImplementsICollection());

            // is
            Assert.True(typeSymbol.IsList());
            Assert.False(typeSymbol.IsArray());
            Assert.False(typeSymbol.IsImmutableArray());

            Assert.Equal("Foo.Customer", typeSymbol.GetContainingTypeFullName());
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
            Assert.True(typeSymbol.ImplementsIEnumerable());
            Assert.True(typeSymbol.ImplementsGenericIEnumerable());
            Assert.True(typeSymbol.ImplementsIList());
            Assert.True(typeSymbol.ImplementsIReadOnlyCollection());
            Assert.True(typeSymbol.ImplementsIReadOnlyList());
            Assert.True(typeSymbol.ImplementsICollection());

            // is
            Assert.True(typeSymbol.IsImmutableArray());
            Assert.False(typeSymbol.IsList());
            Assert.False(typeSymbol.IsArray());

            Assert.Equal("Foo.Customer", typeSymbol.GetContainingTypeFullName());
        }

        static ITypeSymbol GetConversionTypeSymbol(IArgumentOperation argumentOperation)
        {
            var conv = argumentOperation.Value as IConversionOperation;
            Assert.NotNull(conv);
            var typeSymbol = conv.Operand.Type;
            Assert.NotNull(typeSymbol);
            return typeSymbol;
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

            var arg = invocationOperation.Arguments[argumentIndex];
            Assert.NotNull(arg);
            return arg;
        }

        static string BuildDapperCodeText(string implementation) => $$"""
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
                }
            }
        """;
    }
}