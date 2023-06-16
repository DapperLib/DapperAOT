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
            var castType = containingType.GetTypeDisplayName() + "[]";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.IsList())
        {
            var containingType = parameterType.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.List<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.IsImmutableArray())
        {
            var containingType = parameterType.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Immutable.ImmutableArray<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.ImplementsIList(out var listTypeSymbol))
        {
            var containingType = listTypeSymbol.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.IList<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.ImplementsICollection(out var collectionTypeSymbol))
        {
            var containingType = collectionTypeSymbol.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.ICollection<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.ImplementsIReadOnlyList(out var readonlyListTypeSymbol))
        {
            var containingType = readonlyListTypeSymbol.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.IReadOnlyList<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.ImplementsIReadOnlyCollection(out var readonlyCollectionTypeSymbol))
        {
            var containingType = readonlyCollectionTypeSymbol.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.IReadOnlyCollection<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        if (parameterType.ImplementsIEnumerable(out var enumerableTypeSymbol))
        {
            var containingType = enumerableTypeSymbol.GetContainingTypeSymbol();
            var castType = "global::System.Collections.Generic.IEnumerable<" + containingType.GetTypeDisplayName() + ">";
            return WriteMultiExecExpression(containingType!, castType);
        }

        // no multi type found to cast to, so not using multiexec
        return false;

        bool WriteMultiExecExpression(ITypeSymbol containingTypeSymbol, string castType)
        {
            // return Batch<type>
            sb.Append("return global::Dapper.DapperAotExtensions.Batch<")
              .Append(containingTypeSymbol.GetTypeDisplayName())
              .Append('>');

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
                    parameterTypes.Add(containingTypeSymbol, parameterTypeIndex = parameterTypes.Count);
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