using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Globalization;

namespace Dapper.Internal;

internal sealed class MaterializerTracker
{
    readonly Dictionary<INamedTypeSymbol, string> materializers = new(SymbolEqualityComparer.Default);
    readonly Dictionary<string, int> nameCounter = new Dictionary<string, int>();

    public string GetMaterializerName(INamedTypeSymbol symbol)
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

    internal void Add(INamedTypeSymbol? symbol)
    {
        if (symbol is not null) GetMaterializerName(symbol);
    }

    public Dictionary<INamedTypeSymbol, string>.KeyCollection Symbols => materializers.Keys;

    public int Count => materializers.Count;
}
