﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Dapper.CodeAnalysis;

internal abstract class DiagnosticsBase
{
    public static readonly DiagnosticDescriptor UnknownError = LibraryWarning("DAP999", "Unknown analyzer error", "This isn't you; this is me; please log it! '{0}', '{1}'", true);

    protected const string DocsRoot = "https://aot.dapperlib.dev/", RulesRoot = DocsRoot + "rules/";

    private static DiagnosticDescriptor Create(string id, string title, string messageFormat, string category, DiagnosticSeverity severity, bool docs) =>
        new(id, title,
            messageFormat, category, severity, true, helpLinkUri: docs ? (RulesRoot + id) : null);

    protected static DiagnosticDescriptor LibraryWarning(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Library, DiagnosticSeverity.Warning, docs);

    protected static DiagnosticDescriptor LibraryError(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Library, DiagnosticSeverity.Error, docs);

    protected static DiagnosticDescriptor LibraryHidden(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Library, DiagnosticSeverity.Hidden, docs);

    protected static DiagnosticDescriptor LibraryInfo(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Library, DiagnosticSeverity.Info, docs);

    protected static DiagnosticDescriptor SqlWarning(string id, string title, string messageFormat, bool docs = true) => Create(id, title, messageFormat, Category.Sql, DiagnosticSeverity.Warning, docs);

    protected static DiagnosticDescriptor SqlError(string id, string title, string messageFormat, bool docs = true) => Create(id, title, messageFormat, Category.Sql, DiagnosticSeverity.Error, docs);

    protected static DiagnosticDescriptor SqlInfo(string id, string title, string messageFormat, bool docs = true) => Create(id, title, messageFormat, Category.Sql, DiagnosticSeverity.Info, docs);

    protected static DiagnosticDescriptor PerformanceWarning(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Performance, DiagnosticSeverity.Warning, docs);

    protected static DiagnosticDescriptor PerformanceError(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Performance, DiagnosticSeverity.Error, docs);

    protected static DiagnosticDescriptor PerformanceInfo(string id, string title, string messageFormat, bool docs = false) => Create(id, title, messageFormat, Category.Performance, DiagnosticSeverity.Info, docs);

    private static ImmutableDictionary<string, string>? _idsToFieldNames;
    public static bool TryGetFieldName(string id, out string field)
    {
        return (_idsToFieldNames ??= Build()).TryGetValue(id, out field!);
        static ImmutableDictionary<string, string> Build()
            => GetAllFor<DapperInterceptorGenerator.Diagnostics>()
            .Concat(GetAllFor<DapperAnalyzer.Diagnostics>())
            .Concat(GetAllFor<TypeAccessorInterceptorGenerator.Diagnostics>())
            .Distinct()
            .ToImmutableDictionary(x => x.Value.Id, x => x.Key, StringComparer.Ordinal, StringComparer.Ordinal);
    }
    public static ImmutableArray<DiagnosticDescriptor> All<T>() where T : DiagnosticsBase
        => Cache<T>.All;

    private static IEnumerable<KeyValuePair<string, DiagnosticDescriptor>> GetAllFor<T>() where T : DiagnosticsBase
    {
        var fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(DiagnosticDescriptor) && field.GetValue(null) is DiagnosticDescriptor descriptor)
            {
                yield return new(field.Name, descriptor);
            }
        }
    }


    private static class Cache<T> where T : DiagnosticsBase
    {
        public static readonly ImmutableArray<DiagnosticDescriptor> All
            = GetAllFor<T>().Select(x => x.Value).ToImmutableArray();
    }


    private static class Category
    {
        public const string Library = nameof(Library);
        public const string Sql = nameof(Sql);
        public const string Performance = nameof(Performance);
    }
}