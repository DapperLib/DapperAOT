using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Dapper.AOT.Test.Integration.Setup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Dapper.AOT.Test.Integration.Helpers;

internal static class CompilationExtensions
{
    public static Assembly CompileToAssembly(this Compilation compilation)
    {
        using var peStream = new MemoryStream();
        using var pdbstream = new MemoryStream();
        var dbg = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? DebugInformationFormat.Pdb : DebugInformationFormat.PortablePdb;
        
        var emitResult = compilation.Emit(peStream, pdbstream, null, null, null, new EmitOptions(false, dbg));
        if (!emitResult.Success)
        {
            TryThrowErrors(emitResult.Diagnostics);
        }

        peStream.Position = pdbstream.Position = 0;
        return Assembly.Load(peStream.ToArray(), pdbstream.ToArray());
    }

    static void TryThrowErrors(IEnumerable<Diagnostic> items)
    {
        var errors = new List<string>();
        foreach (var item in items)
        {
            if (item.Severity == DiagnosticSeverity.Error)
            {
                errors.Add(item.GetMessage(CultureInfo.InvariantCulture));
            }
        }

        if (errors.Count > 0)
        {
            throw new ExpressionParsingException(errors);
        }
    }
}