using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Dapper.AOT.Test;

/// <summary>
/// Everything in the <c>Dapper.CodeAnalysis.Model</c> namespace is cached by the incremental
/// generator driver, so it must be plain data: a stored <c>ISymbol</c>/<c>SyntaxNode</c>/
/// <c>Location</c>/<c>Compilation</c> both pins entire compilations in memory (a serious leak
/// in a long-running IDE session) and defeats the cache (symbol equality does not hold across
/// compilations). Roslyn <b>value</b> types would be acceptable in principle, but the model
/// currently stores none, so this test forbids Roslyn types outright; loosen deliberately if
/// that ever changes.
/// </summary>
public class ModelShapeTests
{
    const string ModelNamespace = "Dapper.CodeAnalysis.Model";

    public static IEnumerable<object[]> ModelTypes()
        => typeof(Dapper.CodeAnalysis.DapperAnalyzer).Assembly.GetTypes()
            .Where(t => (t.Namespace == ModelNamespace
                    // the cached pipeline states themselves are also model, wherever they live
                    || typeof(Dapper.CodeAnalysis.DapperInterceptorGenerator.SourceState).IsAssignableFrom(t))
                && !t.IsEnum && !IsCompilerGenerated(t))
            .Select(t => new object[] { t });

    static bool IsCompilerGenerated(Type type) => type.Name.StartsWith("<");

    [Fact]
    public void ModelNamespaceIsNotEmpty() => Assert.NotEmpty(ModelTypes());

    [Theory, MemberData(nameof(ModelTypes))]
    public void ModelTypeHoldsNoRoslynReferences(Type type)
    {
        List<string> failures = [];
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Visit(field.FieldType, $"{type.Name}.{field.Name}", failures, []);
        }
        Assert.Empty(failures);
    }

    static void Visit(Type fieldType, string path, List<string> failures, HashSet<Type> seen)
    {
        if (!seen.Add(fieldType)) return;

        if (fieldType.IsArray)
        {
            Visit(fieldType.GetElementType()!, path + "[]", failures, seen);
            return;
        }
        if (fieldType.IsGenericParameter) return; // open generic (e.g. EquatableArray<T>): checked at each closed usage

        var assemblyName = fieldType.Assembly.GetName().Name ?? "";
        if (assemblyName.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
        {
            failures.Add($"{path} stores Roslyn type {fieldType.Name}");
            return;
        }

        if (fieldType.IsGenericType)
        {
            foreach (var arg in fieldType.GetGenericArguments())
            {
                Visit(arg, $"{path}<{arg.Name}>", failures, seen);
            }
        }

        // follow nested model/user types (but not framework primitives)
        if (fieldType.Namespace == ModelNamespace)
        {
            foreach (var field in fieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Visit(field.FieldType, $"{path}.{field.Name}", failures, seen);
            }
        }
    }

    [Theory, MemberData(nameof(ModelTypes))]
    public void ModelTypeIsEquatable(Type type)
    {
        if (type.IsInterface || type.IsAbstract || type.Name == "Enumerator") return; // (the enumerator helper is not a cached value)
        if (typeof(Dapper.CodeAnalysis.DapperInterceptorGenerator.SourceState).IsAssignableFrom(type)
            || type.IsNested)
        {
            // nested/state types use Equals overrides rather than IEquatable; the override is what matters
            var equalsMethod = type.GetMethod("Equals", [type]);
            Assert.True(equalsMethod is not null, type.Name + " should declare structural equality");
            return;
        }
        // structural equality is what makes the incremental cache work at all
        var equatable = typeof(IEquatable<>).MakeGenericType(type);
        Assert.True(equatable.IsAssignableFrom(type), $"{type.Name} should implement IEquatable<{type.Name}>");
    }
}
