using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Dapper.CodeAnalysis;

public sealed partial class DapperInterceptorGenerator
{
    static void WriteMultiMapImplementation(
        CodeWriter sb,
        IMethodSymbol method,
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
        var typeArgs = method.TypeArguments;
        var arity = typeArgs.Length;
        
        sb.Append("return ");
        if (flags.HasAll(OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
        {
            sb.Append("global::Dapper.DapperAotExtensions.AsEnumerableAsync(").Indent(false).NewLine();
        }
        
        // Create the Command<TArgs>
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
        {
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
        
        // Call the appropriate QueryBuffered/QueryBufferedAsync/QueryUnbuffered/QueryUnbufferedAsync method
        bool isAsync = flags.HasAny(OperationFlags.Async);
        bool isUnbuffered = flags.HasAny(OperationFlags.Unbuffered);
        
        if (isUnbuffered)
        {
            sb.Append("QueryUnbuffered");
        }
        else
        {
            sb.Append("QueryBuffered");
        }
        
        if (isAsync)
        {
            sb.Append("Async");
        }
        
        // Add type parameters <T1, T2, ..., TReturn>
        sb.Append("<");
        for (int i = 0; i < arity; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(typeArgs[i]);
        }
        sb.Append(">(");
        
        // Add arguments: args, map, factory1, factory2, ..., splitOn, rowCountHint
        WriteTypedArg(sb, parameterType).Append(", ");
        sb.Append(Forward(methodParameters, "map")).Append(", ");
        
        // Add row factories for each type (except the return type)
        for (int i = 0; i < arity - 1; i++)
        {
            var typeArg = typeArgs[i];
            sb.AppendReader(typeArg, readers, flags, additionalCommandState?.QueryColumns ?? default);
            sb.Append(", ");
        }
        
        // Add splitOn parameter
        if (HasParam(methodParameters, "splitOn"))
        {
            sb.Append(Forward(methodParameters, "splitOn"));
        }
        else
        {
            sb.Append("\"Id\"");
        }
        
        // Add rowCountHint if needed (only for buffered queries)
        if (!isUnbuffered && flags.HasAny(OperationFlags.Query) && additionalCommandState is { HasRowCountHint: true })
        {
            if (additionalCommandState.RowCountHintMemberName is null)
            {
                sb.Append(", rowCountHint: ").Append(additionalCommandState.RowCountHint);
            }
            else
            {
                // member-based hint; add after args
                sb.Append(", rowCountHint: ").Append("args.").Append(additionalCommandState.RowCountHintMemberName);
            }
        }
        
        // Add cancellationToken for async
        if (isAsync && HasParam(methodParameters, "cancellationToken"))
        {
            sb.Append(", cancellationToken: ").Append(Forward(methodParameters, "cancellationToken"));
        }
        
        if (flags.HasAll(OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
        {
            sb.Append(")").Outdent(false);
        }
        sb.Append(")");
        sb.Append(";").NewLine();
    }
}
