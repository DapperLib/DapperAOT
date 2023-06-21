using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dapper.SqlAnalysis;

internal static class TSqlParser
{
    public static string[] GetArgs(string sql, out int errorCount)
    {
        using (var reader = new StringReader(sql))
        {
            var parser = new TSql160Parser(true, SqlEngineType.All);
            var tree = parser.Parse(reader, out var errors);
            errorCount = errors.Count;
            var visitor = new MyVisitor();
            tree.Accept(visitor);
            return visitor.GetParameters();
        }
    }

    class MyVisitor : TSqlFragmentVisitor
    {
        public string[] GetParameters()
        {
            var arr = missing.ToArray();
            Array.Sort(arr);
            return arr;
        }
        HashSet<string> declared = new(), missing = new();
        public override void ExplicitVisit(DeclareVariableStatement node)
        {
            foreach (var dec in node.Declarations)
            {
                var name = dec.VariableName.Value;
                if (declared.Add(name))
                {
                    missing.Remove(name);
                }
            }
            base.ExplicitVisit(node);
        }
        public override void ExplicitVisit(VariableReference node)
        {
            var name = node.Name;
            if (!declared.Contains(name))
            {
                missing.Add(name);
            }
            base.ExplicitVisit(node);
        }
    }
}
