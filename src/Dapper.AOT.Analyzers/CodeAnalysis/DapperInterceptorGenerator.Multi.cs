using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Dapper.CodeAnalysis;

public sealed partial class DapperInterceptorGenerator
{
    private static bool TryWriteMultiExecImplementation(
        CodeWriter sb,
        OperationFlags flags,
        OperationFlags commandTypeMode,
        ITypeSymbol? parameterType,
        string map, bool cache,
        ImmutableArray<IParameterSymbol> methodParameters,
        CommandFactoryState factories)
    {
        if (!HasAny(flags, OperationFlags.Execute))
        {
            // only EXECUTE is supported for collections
            return false;
        }

        if (Inspection.IsCollectionType(parameterType, out var elementType, out var castType))
        {
            WriteMultiExecExpression(elementType!, castType);
            return true;
        }

        // no multi type found to cast to, so not using multiexec
        return false;

        void WriteMultiExecExpression(ITypeSymbol elementType, string castType)
        {
            // return Command<type>
            sb.Append("return global::Dapper.DapperAotExtensions.Command");

            // return Command<type>(...).
            WriteBatchCommandArguments(elementType);

            // return Command<type>(...).ExecuteAsync((cast)param, ...);
            bool isAsync = HasAny(flags, OperationFlags.Async);
            sb.Append("Execute").Append(isAsync ? "Async" : "").Append("(");
            sb.Append("(").Append(castType).Append(")param").Append(");");
            sb.NewLine().Outdent().NewLine().NewLine();
        }

        void WriteBatchCommandArguments(ITypeSymbol elementType)
        {
            // cnn, transaction, sql
            sb.Append("(cnn, ").Append(Forward(methodParameters, "transaction")).Append(", sql, ");

            // commandType
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

            // timeout
            sb.Append(", ")
              .Append(Forward(methodParameters, "commandTimeout"))
              .Append(HasParam(methodParameters, "commandTimeout") ? " ?? -1" : "")
              .Append(", ");

            // commandFactory
            if (HasAny(flags, OperationFlags.HasParameters))
            {
                var index = factories.GetIndex(elementType, map, cache, out var subIndex);
                sb.Append("CommandFactory").Append(index).Append(".Instance").Append(subIndex);
            }
            else
            {
                sb.Append("DefaultCommandFactory");
            }
            sb.Append(").");
        }
    }
}