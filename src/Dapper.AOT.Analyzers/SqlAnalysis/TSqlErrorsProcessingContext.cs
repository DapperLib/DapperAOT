using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper.Internal;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dapper.SqlAnalysis;

internal struct TSqlErrorsProcessingContext
{
    readonly IList<ParseError> _parseErrors;
    
    ISet<int>? _pseudoPositionalArgumentsOffsets;
    
    public TSqlErrorsProcessingContext(IList<ParseError> parseErrors)
    {
        _parseErrors = parseErrors;
    }

    public IEnumerable<ParseError> GetErrorsToReport(string sql)
    {
        if (_parseErrors is null || _parseErrors.Count == 0) yield break;
        foreach (var parseError in _parseErrors)
        {
            if (IsPseudoPositionalParameterGenuineUsage(sql, parseError))
            {
                continue;
            }
            
            yield return parseError;
        }
    }
    
    bool IsPseudoPositionalParameterGenuineUsage(string sql, ParseError error)
    {
        if (error.Number != 46010) return false; // `46010` is `Incorrect syntax around ...`
        if (_pseudoPositionalArgumentsOffsets is null)
        {
            var regexMatch = CompiledRegex.PseudoPositional.Match(sql);
            if (!regexMatch!.Success || regexMatch.Groups.Count == 0)
            {
                _pseudoPositionalArgumentsOffsets = new HashSet<int>();
                return false;
            }

            _pseudoPositionalArgumentsOffsets = new HashSet<int>();
            foreach (var match in regexMatch.Groups.OfType<Match>())
            {
                _pseudoPositionalArgumentsOffsets.Add(match.Index);
            }
        }

        if (_pseudoPositionalArgumentsOffsets.Count == 0)
        {
            // regex would find positional arguments, if there are any
            return false;
        }

        return _pseudoPositionalArgumentsOffsets.Contains(error.Offset);
    }
}