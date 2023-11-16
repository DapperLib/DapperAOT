using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Dapper.CodeAnalysis;

public sealed partial class DapperInterceptorGenerator
{
    static void WriteSingleImplementation(
        CodeWriter sb,
        IMethodSymbol method,
        ITypeSymbol? resultType,
        OperationFlags flags,
        OperationFlags commandTypeMode,
        ITypeSymbol? parameterType,
        string map, bool cache,
        in ImmutableArray<IParameterSymbol> methodParameters,
        in CommandFactoryState factories,
        in RowReaderState readers,
        string? fixedSql,
        AdditionalCommandState? additionalCommandState)
    {
        sb.Append("return ");
        if (flags.HasAll(OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
        {
            sb.Append("global::Dapper.DapperAotExtensions.AsEnumerableAsync(").Indent(false).NewLine();
        }
        // (DbConnection connection, DbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, CommandFactory<TArgs>? commandFactory)
        sb.Append("global::Dapper.DapperAotExtensions.Command(cnn, ").Append(Forward(methodParameters, "transaction")).Append(", ");
        if (fixedSql is not null)
        {
            sb.AppendVerbatimLiteral(fixedSql).Append(", ");
        }
        else
        {
            sb.Append("sql, ");
        }
        if (commandTypeMode == 0)
        {   // not hard-coded
            if (HasParam(methodParameters, "command"))
            {
                sb.Append("command.GetValueOrDefault()");
            }
            else
            {
                sb.Append("default");
            }
        }
        else
        {
            sb.Append("global::System.Data.CommandType.").Append(commandTypeMode.ToString());
        }
        sb.Append(", ").Append(Forward(methodParameters, "commandTimeout")).Append(HasParam(methodParameters, "commandTimeout") ? ".GetValueOrDefault()" : "").Append(", ");
        if (flags.HasAny(OperationFlags.HasParameters))
        {
            var index = factories.GetIndex(parameterType!, map, cache, additionalCommandState, out var subIndex);
            sb.Append("CommandFactory").Append(index).Append(".Instance").Append(subIndex);
        }
        else
        {
            sb.Append("DefaultCommandFactory");
        }
        sb.Append(").");

        bool isAsync = flags.HasAny(OperationFlags.Async);
        if (flags.HasAny(OperationFlags.Query))
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
            }).Append(isAsync ? "Async" : "").Append("(");
            WriteTypedArg(sb, parameterType).Append(", ");
            if (!flags.HasAny(OperationFlags.SingleRow))
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
            sb.AppendReader(resultType, readers);
        }
        else if (flags.HasAny(OperationFlags.Execute))
        {
            sb.Append("Execute");
            if (flags.HasAny(OperationFlags.Scalar))
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
        if (flags.HasAny(OperationFlags.Query) && additionalCommandState is { HasRowCountHint: true })
        {
            if (additionalCommandState.RowCountHintMemberName is null)
            {
                sb.Append(", rowCountHint: ").Append(additionalCommandState.RowCountHint);
            }
            else if (parameterType is not null && !parameterType.IsAnonymousType)
            {
                sb.Append(", rowCountHint: ((").Append(parameterType).Append(")param!).").Append(additionalCommandState.RowCountHintMemberName);
            }
        }
        if (isAsync && HasParam(methodParameters, "cancellationToken"))
        {
            sb.Append(", cancellationToken: ").Append(Forward(methodParameters, "cancellationToken"));
        }
        if (flags.HasAll(OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
        {
            sb.Append(")").Outdent(false);
        }
        sb.Append(")");
        if (flags.HasAny(OperationFlags.Scalar) || (flags.HasAny(OperationFlags.SingleRow) && !flags.HasAny(OperationFlags.AtLeastOne)))
        {
            // there are some NRT oddities in Dapper itself; shim over everything
            // (we know that DapperAOT has "T? {First|Single}OrDefault<T>[Async]" and "T? ExecuteScalar<T>[Async]")
            bool addNullForgiving;
            if (method.ReturnType.IsAsync(out var t))
            {
                addNullForgiving = t is not null && t.NullableAnnotation != NullableAnnotation.Annotated;
            }
            else
            {
                addNullForgiving = method.ReturnType.NullableAnnotation != NullableAnnotation.Annotated;
            }
            if (addNullForgiving)
            {
                sb.Append("!");
            }
        }
        sb.Append(";").NewLine();

        static CodeWriter WriteTypedArg(CodeWriter sb, ITypeSymbol? parameterType)
        {
            if (parameterType is null || parameterType.IsAnonymousType)
            {
                sb.Append("param");
            }
            else
            {
                sb.Append("(").Append(parameterType).Append(")param!");
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