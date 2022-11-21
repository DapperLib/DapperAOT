using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Globalization;

namespace Dapper.Internal;

internal sealed class MaterializerTracker
{
    readonly Dictionary<ITypeSymbol, string> materializers = new(SymbolEqualityComparer.Default);
    readonly Dictionary<string, int> nameCounter = new Dictionary<string, int>();
    public string Namespace { get; }
    public MaterializerTracker(string @namespace)
        => Namespace = @namespace;

    public string GetMaterializerName(ITypeSymbol symbol)
    {
        if (materializers.TryGetValue(symbol, out var name)) return name;

        name = symbol.Name;
        if (nameCounter.TryGetValue(name, out var count))
        {
            nameCounter[name] = ++count;
            name = name + count.ToString(CultureInfo.InvariantCulture);
            materializers.Add(symbol, name);
            return name;
        }

        nameCounter.Add(name, 0);
        materializers.Add(symbol, name);
        return name;
    }

    internal void Add(ITypeSymbol? symbol)
    {
        if (symbol is not null) GetMaterializerName(symbol);
    }

    public Dictionary<ITypeSymbol, string>.KeyCollection Symbols => materializers.Keys;

    public int Count => materializers.Count;
}
