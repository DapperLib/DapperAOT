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
        ImmutableArray<IParameterSymbol> methodParameters,
        IDictionary<ITypeSymbol, int> parameterTypes,
        IDictionary<ITypeSymbol, int> resultTypes)
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
            // return Batch<type>
            sb.Append("return global::Dapper.DapperAotExtensions.Batch<")
              .Append(elementType.GetTypeDisplayName())
              .Append('>');

            // return Batch<type>(...).
            WriteBatchCommandArguments(elementType);

            // return Batch<type>(...).ExecuteAsync((cast)param, ...);
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
                sb.Append(Forward(methodParameters, "commandType")).Append(HasParam(methodParameters, "commandType") ? ".GetValueOrDefault()" : "");
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
                if (!parameterTypes.TryGetValue(elementType, out var parameterTypeIndex))
                {
                    parameterTypes.Add(elementType, parameterTypeIndex = parameterTypes.Count);
                }
                sb.Append("CommandFactory").Append(parameterTypeIndex).Append(".Instance");
            }
            else
            {
                sb.Append("DefaultCommandFactory");
            }
            sb.Append(").");
        }
    }
}