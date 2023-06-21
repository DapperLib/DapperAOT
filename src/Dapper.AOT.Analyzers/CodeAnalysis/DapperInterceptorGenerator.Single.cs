using Dapper.Internal;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Dapper.CodeAnalysis;

public sealed partial class DapperInterceptorGenerator
{
    void WriteSingleImplementation(
        CodeWriter sb,
        IMethodSymbol method,
        ITypeSymbol? resultType,
        OperationFlags flags,
        OperationFlags commandTypeMode,
        ITypeSymbol? parameterType,
        string map, bool cache,
        ImmutableArray<IParameterSymbol> methodParameters,
        CommandFactoryState factories,
        RowReaderState readers)
    {
        sb.Append("return ");
        if (HasAll(flags, OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
        {
            sb.Append("global::Dapper.DapperAotExtensions.AsEnumerableAsync(").Indent(false).NewLine();
        }
        // (DbConnection connection, DbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, CommandFactory<TArgs>? commandFactory)
        sb.Append("global::Dapper.DapperAotExtensions.Command<");
        if (parameterType is null || parameterType.IsAnonymousType)
        {
            sb.Append("object?");
        }
        else
        {
            sb.Append(parameterType);
        }
        sb.Append(">(cnn, ").Append(Forward(methodParameters, "transaction")).Append(", sql, ");
        if (commandTypeMode == 0)
        {   // not hard-coded
            if (HasParam(methodParameters, "command"))
            {
                sb.Append("command ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)");
            }
            else
            {
                sb.Append("global::Dapper.DapperAotExtensions.GetCommandType(sql)");
            }
        }
        else
        {
            sb.Append("global::System.Data.CommandType.").Append(commandTypeMode.ToString());
        }
        sb.Append(", ").Append(Forward(methodParameters, "commandTimeout")).Append(HasParam(methodParameters, "commandTimeout") ? " ?? -1" : "").Append(", ");
        if (HasAny(flags, OperationFlags.HasParameters))
        {
            var index = factories.GetIndex(parameterType!, map, cache, out var subIndex);
            sb.Append("CommandFactory").Append(index).Append(".Instance").Append(subIndex);
        }
        else
        {
            sb.Append("DefaultCommandFactory");
        }
        sb.Append(").");

        bool isAsync = HasAny(flags, OperationFlags.Async);
        if (HasAny(flags, OperationFlags.Query))
        {
            sb.Append("Query").Append((flags & (OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne)) switch
            {
                OperationFlags.SingleRow => "FirstOrDefault",
                OperationFlags.SingleRow | OperationFlags.AtLeastOne => "First",
                OperationFlags.SingleRow | OperationFlags.AtMostOne => "SingleOrDefault",
                OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne => "Single",
                _ => "",
            }).Append((flags & (OperationFlags.Buffered | OperationFlags.Unbuffered)) switch
            {
                OperationFlags.Buffered => "Buffered",
                OperationFlags.Unbuffered => "Unbuffered",
                _ => ""
            }).Append(isAsync ? "Async" : "").Append("<").Append(resultType).Append(">").Append("(");
            WriteTypedArg(sb, parameterType).Append(", ");
            if (!HasAny(flags, OperationFlags.SingleRow))
            {
                switch (flags & (OperationFlags.Buffered | OperationFlags.Unbuffered))
                {
                    case OperationFlags.Buffered:
                    case OperationFlags.Unbuffered:
                        break;
                    default: // using "could be either" buffer mode
                        sb.Append(Forward(methodParameters, "buffered")).Append(", ");
                        break;
                }
            }
            sb.Append("RowFactory").Append(readers.GetIndex(resultType!)).Append(".Instance");
        }
        else if (HasAny(flags, OperationFlags.Execute))
        {
            sb.Append("Execute");
            if (HasAny(flags, OperationFlags.Scalar))
            {
                sb.Append("Scalar");
            }
            sb.Append(isAsync ? "Async" : "");
            if (method.Arity == 1)
            {
                sb.Append("<").Append(resultType).Append(">");
            }
            sb.Append("(");
            WriteTypedArg(sb, parameterType);
        }
        else
        {
            sb.NewLine().Append("#error not supported: ").Append(method.Name).NewLine();
        }
        if (isAsync)
        {
            sb.Append(", ").Append(Forward(methodParameters, "cancellationToken"));
        }
        if (HasAll(flags, OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
        {
            sb.Append(")").Outdent(false);
        }
        sb.Append(");");
        sb.NewLine().Outdent().NewLine().NewLine();

        static CodeWriter WriteTypedArg(CodeWriter sb, ITypeSymbol? parameterType)
        {
            if (parameterType is null || parameterType.IsAnonymousType)
            {
                sb.Append("param");
            }
            else
            {
                sb.Append("(").Append(parameterType).Append(")param");
            }
            return sb;
        }
    }

    private static bool HasParam(ImmutableArray<IParameterSymbol> methodParameters, string name)
    {
        foreach (var p in methodParameters)
        {
            if (p.Name == name)
            {
                return true;
            }
        }
        return false;
    }

    private static string Forward(ImmutableArray<IParameterSymbol> methodParameters, string name)
        => HasParam(methodParameters, name) ? name : "default";
}