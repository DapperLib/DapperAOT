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

        if (parameterType.IsArray())
        {
            var containingType = parameterType.GetContainingTypeSymbol();
            var castType = parameterType.GetContainingTypeFullName() + "[]";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.IsList())
        {
            var containingType = parameterType.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.List<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.IsImmutableArray())
        {
            var containingType = parameterType.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Immutable.ImmutableArray<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.ImplementsIList())
        {
            var castType = "global::System.Collections.Generic.IList<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(castType);
        }

        if (parameterType.ImplementsICollection())
        {
            var castType = "global::System.Collections.Generic.ICollection<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(castType);
        }

        if (parameterType.ImplementsIReadOnlyList())
        {
            var castType = "global::System.Collections.Generic.IReadOnlyList<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(castType);
        }

        if (parameterType.ImplementsIReadOnlyCollection())
        {
            var castType = "global::System.Collections.Generic.IReadOnlyCollection<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(castType);
        }

        if (parameterType.ImplementsGenericIEnumerable())
        {
            var castType = "global::System.Collections.Generic.IEnumerable<" + parameterType.GetContainingTypeFullName() + ">";
            return WriteMultiExecExpression(castType);
        }

        // no multi type found to cast to, so not using multiexec
        return false;

        bool WriteMultiExecExpression(ITypeSymbol containingTypeSymbol, string castType)
        {
            // return cnn.Batch(transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((List<T>)param);

            // return Batch<type>
            sb.Append("return global::Dapper.DapperAotExtensions.Batch<");
            if (parameterType is null || parameterType.IsAnonymousType) sb.Append("object?");
            else sb.Append(parameterType.GetContainingTypeFullName());
            sb.Append('>');

            // return Batch<type>(...).
            WriteBatchCommandArguments(containingTypeSymbol);

            // return Batch<type>(...).ExecuteAsync((cast)param, ...);
            bool isAsync = HasAny(flags, OperationFlags.Async);
            sb.Append("Execute").Append(isAsync ? "Async" : "").Append("(");
            sb.Append("(").Append(castType).Append(")param").Append(");");
            sb.NewLine().Outdent().NewLine().NewLine();

            return true;
        }

        void WriteBatchCommandArguments(ITypeSymbol containingTypeSymbol)
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
                if (!parameterTypes.TryGetValue(containingTypeSymbol, out var parameterTypeIndex))
                {
                    parameterTypes.Add(containingTypeSymbol, parameterTypeIndex = resultTypes.Count);
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