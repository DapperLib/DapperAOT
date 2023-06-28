using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Dapper.Internal;

internal sealed class CodeWriter
{
    static CodeWriter? s_Spare;
    public static CodeWriter Create()
        => Interlocked.Exchange(ref s_Spare, null) ?? new CodeWriter();

    private int _indent;
    private bool _isLineEmpty = true;
    public CodeWriter Clear()
    {
        _sb.Clear();
        _indent = 0;
        _isLineEmpty = true;
        return this;
    }
    private readonly StringBuilder _sb = new();

    public int Length
    {
        get => _sb.Length;
        set => _sb.Length = value;
    }
    private StringBuilder Core
    {
        get
        {
            if (_isLineEmpty)
            {
                for (int i = 0; i < _indent; i++)
                {
                    _sb.Append(Tab);
                }
                _isLineEmpty = false;
            }
            return _sb;
        }
    }

    public string Tab { get; set; } = "    "; // "\t"

    public CodeWriter Append(bool value) => Append(value ? "true" : "false");
    public CodeWriter Append(int? value) => value.HasValue ? Append(value.GetValueOrDefault()) : this;

    public CodeWriter Append(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Core.Append(value);
        }
        return this;
    }

    public static string GetTypeName(ITypeSymbol? value)
    {
        if (value is null) return "(none)";
        if (value.IsAnonymousType) return value.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        string s = value.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (value is INamedTypeSymbol named && named.Arity == 1 && named.TypeArguments[0].NullableAnnotation == NullableAnnotation.Annotated
            && s.EndsWith(">") && !s.EndsWith("?>"))
        {
            // weird glitch in FQF - doesn't always add the annotation - example: Task<dynamic?>
            s = s.Substring(0, s.Length - 1) + "?>";
        }

        if (value.NullableAnnotation == NullableAnnotation.Annotated && !s.EndsWith("?"))
        {
            s += "?"; // weird glitch in FQF - doesn't always add the annotation - example: dynamic?
        }
        return s;
    }

    public CodeWriter Append(ITypeSymbol? value, bool anonToTuple = false)
    {
        if (value is null)
        { }
        else if (value.IsAnonymousType)
        {
            if (anonToTuple)
            {
                AppendAsValueTuple(value);
            }
            else
            {
                Append(value.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
        }
        else
        {
            Append(GetTypeName(value));
        }
        return this;
    }

    private void AppendAsValueTuple(ITypeSymbol value)
    {
        var members = value.GetMembers();
        int count = CountGettableInstanceMembers(members);
        Append(count switch
        {
            0 => "object?",
            1 => "",
            _ => "("
        });
        bool isFirst = true;
        foreach (var member in members)
        {
            if (IsGettableInstanceMember(member, out var memberType))
            {
                if (!isFirst)
                {
                    Append(", ");
                }
                isFirst = false;
                Append(memberType, true);
                if (count != 1)
                {
                    Append(" ").Append(member.Name);
                }
            }
        }
        if (count > 1)
        {
            Append(")");
        }
    }

    public static int CountGettableInstanceMembers(ImmutableArray<ISymbol> members)
    {
        int count = 0;
        foreach (var member in members)
        {
            if (IsGettableInstanceMember(member, out _)) count++;
        }
        return count;
    }

    public static bool IsGettableInstanceMember(ISymbol symbol, out ITypeSymbol type)
    {
        if (symbol.DeclaredAccessibility == Accessibility.Public && !symbol.IsStatic)
        {
            switch (symbol)
            {
                case IPropertySymbol prop when !prop.IsIndexer && prop.GetMethod is { DeclaredAccessibility: Accessibility.Public }:
                    type = prop.Type;
                    return true;
                case IFieldSymbol field:
                    type = field.Type;
                    return true;
            }
        }
        type = default!;
        return false;
    }

    public static bool IsSettableInstanceMember(ISymbol symbol, out ITypeSymbol type)
    {
        if (symbol.DeclaredAccessibility == Accessibility.Public && !symbol.IsStatic)
        {
            switch (symbol)
            {
                case IPropertySymbol prop when !prop.IsIndexer && prop.SetMethod is { DeclaredAccessibility: Accessibility.Public }:
                    type = prop.Type;
                    return true;
                case IFieldSymbol field when !field.IsReadOnly:
                    type = field.Type;
                    return true;
            }
        }
        type = default!;
        return false;
    }

    public CodeWriter AppendEnumLiteral(ITypeSymbol enumType, int value)
    {
        foreach (var member in enumType.GetMembers())
        {
            if (member is IFieldSymbol field && field.IsStatic && field.HasConstantValue && field.ConstantValue is int test
                && test == value)
            {
                return Append(enumType).Append(".").Append(field.Name);
            }
        }
        return Append("(").Append(enumType).Append(")").Append(value).Append("); ");

    }
    public CodeWriter AppendVerbatimLiteral(string? value) => Append(
        value is null ? "null" : SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value)).ToFullString());
    public CodeWriter Append(char value)
    {
        Core.Append(value);
        return this;
    }
    internal CodeWriter Append(ReadOnlySpan<char> value)
    {
        if (!value.IsEmpty)
        {
#if NETSTANDARD2_0
            unsafe
            {
                fixed (char* ptr = value)
                {
                    Core.Append(ptr, value.Length);
                }
            }
#else
            sb.Append(value);
#endif
        }
        return this;
    }

    internal CodeWriter Append(int value)
    {
        Core.Append(value.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    internal CodeWriter Append(uint value)
    {
        Core.Append(value.ToString(CultureInfo.InvariantCulture)).Append('U');
        return this;
    }

    public CodeWriter NewLine()
    {
        _sb.AppendLine();
        _isLineEmpty = true;
        return this;
    }

    public CodeWriter Indent(bool withScope = true)
    {
        if (withScope) NewLine().Append("{");
        _indent++;
        return this;
    }
    public CodeWriter Outdent(bool withScope = true)
    {
        _indent--;
        if (withScope) NewLine().Append("}");
        return this;
    }

    public CodeWriter DisableWarning(string warning)
    {
        NewLine();
        _sb.Append("#pragma warning disable ").AppendLine(warning);
        return this;
    }

    public CodeWriter RestoreWarning(string warning)
    {
        NewLine();
        _sb.Append("#pragma warning restore ").AppendLine(warning);
        return this;
    }
    public CodeWriter DisableObsolete()
        => DisableWarning("CS0612, CS0618 // obsolete");
    public CodeWriter RestoreObsolete()
        => RestoreWarning("CS0612, CS0618 // obsolete");


    [Obsolete("You probably mean " + nameof(ToStringRecycle))]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override string ToString()
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        => _sb.ToString();
    public string ToStringRecycle()
    {
        var s = _sb.ToString();
        Clear();
        Interlocked.Exchange(ref s_Spare, this);
        return s;
    }
}
