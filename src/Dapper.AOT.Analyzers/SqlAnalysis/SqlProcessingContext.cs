using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper.Internal;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dapper.SqlAnalysis;

internal readonly struct SqlProcessingContext
{
    private readonly TSqlProcessor.VariableTrackingVisitor _variableVisitor;
    private readonly IReadOnlyDictionary<int, string> _pseudoPositionalArgumentsOffsetNames;
    
    public SqlProcessingContext(TSqlProcessor.VariableTrackingVisitor variableVisitor, string sql)
    {
        _variableVisitor = variableVisitor;
        _pseudoPositionalArgumentsOffsetNames = BuildPseudoPositionalArgsOffsetNamesMap();
            
        IReadOnlyDictionary<int, string> BuildPseudoPositionalArgsOffsetNamesMap()
        {
            var offsetNamesMap = new Dictionary<int, string>();
            var regexMatch = CompiledRegex.PseudoPositional.Match(sql);
            while (regexMatch.Success)
            {
                foreach (var match in regexMatch.Groups.OfType<Match>())
                {
                    offsetNamesMap.Add(match.Index, match.Value.Trim('?'));
                }

                regexMatch = regexMatch.NextMatch();
            }

            return offsetNamesMap;
        }
    }
    
    public IEnumerable<ParseError> GetErrorsToReport(IList<ParseError>? parseErrors)
    {
        if (parseErrors is null || parseErrors.Count == 0) yield break;
        foreach (var parseError in parseErrors)
        {
            if (_pseudoPositionalArgumentsOffsetNames.Count != 0 && IsPseudoPositionalParameterGenuineUsage(parseError))
            {
                continue;
            }
            
            yield return parseError;
        }
    }

    public void MarkPseudoPositionalVariablesUsed(in ISet<string> parameters)
    {
        foreach (var name in _pseudoPositionalArgumentsOffsetNames.Values)
        {
            if (_variableVisitor.TryGetByName(name, out var variable) && parameters.Contains(name))
            {
                var usedVar = variable.WithUsed();
                usedVar = usedVar.WithConsumed();
                _variableVisitor.SetVariable(usedVar);
            }
        }
    }
    
    bool IsPseudoPositionalParameterGenuineUsage(ParseError error)
    {
        if (error.Number != 46010) return false; // `46010` is `Incorrect syntax around ...`
        return _pseudoPositionalArgumentsOffsetNames.ContainsKey(error.Offset);
    }
}