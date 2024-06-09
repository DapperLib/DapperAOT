using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper.Internal;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dapper.SqlAnalysis;

internal readonly struct TSqlSpecialScenariosProcessingContext
{
    private readonly TSqlProcessor.VariableTrackingVisitor _variableVisitor;
    
    private readonly IDictionary<int, string> _pseudoPositionalArgumentsOffsetNames;
    
    public TSqlSpecialScenariosProcessingContext(TSqlProcessor.VariableTrackingVisitor variableVisitor, string sql)
    {
        _variableVisitor = variableVisitor;
        
        var regexMatch = CompiledRegex.PseudoPositional.Match(sql);
        if (!regexMatch!.Success)
        {
            _pseudoPositionalArgumentsOffsetNames = new Dictionary<int, string>();
        }
        
        _pseudoPositionalArgumentsOffsetNames = new Dictionary<int, string>();
        foreach (var match in regexMatch.Groups.OfType<Match>())
        {
            _pseudoPositionalArgumentsOffsetNames.Add(match.Index, match.Value.Trim('?'));
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
        if (_pseudoPositionalArgumentsOffsetNames.Count == 0)
        {
            // regex would not find positional arguments, if there are any
            return false;
        }

        return _pseudoPositionalArgumentsOffsetNames.ContainsKey(error.Offset);
    }
}