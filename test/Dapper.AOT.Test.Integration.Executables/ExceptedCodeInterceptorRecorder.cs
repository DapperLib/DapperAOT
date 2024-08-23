using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dapper.AOT.Test.Integration.Executables.Recording;
using Dapper.CodeAnalysis;

namespace Dapper.AOT.Test.Integration.Executables;

public class ExceptedCodeInterceptorRecorder<TExecutable> : IInterceptorRecorder
{
    readonly string expectedFileName;

    public ExceptedCodeInterceptorRecorder(string expectedFileName)
    {
        this.expectedFileName = expectedFileName ?? throw new ArgumentNullException(nameof(expectedFileName));
    }

    public bool WasCalled { get; private set; }
    public string? Diagnostics { get; private set; }

    public void Record()
    {
        WasCalled = true;
        var diagnostics = new StringBuilder();
        
        var stackTrace = new StackTrace(fNeedFileInfo: true);
        var recentFrames = stackTrace.GetFrames().Take(15).ToList(); // we dont need everything

        var userCodeFrameIndex = recentFrames.FindIndex(frame => 
            frame.GetFileName()?.Contains(expectedFileName) == true && frame.GetMethod()?.Name.Equals(nameof(IExecutable<TExecutable>.Execute)) == true);
        if (userCodeFrameIndex == -1)
        {
            diagnostics.AppendLine("- User code execution is not found");
        }
        
        var dapperInterceptionFrameIndex = recentFrames.FindIndex(frame =>
            frame.GetFileName()?.Contains(".generated.cs") == true && frame.GetFileName()?.Contains(nameof(DapperInterceptorGenerator)) == true);
        if (dapperInterceptionFrameIndex == -1)
        {
            diagnostics.AppendLine("- User code execution is not found");
        }

        if (userCodeFrameIndex < dapperInterceptionFrameIndex)
        {
            diagnostics.AppendLine("- User code call should be higher (executed before) on the stack trace than intercepted code");
        }

        Diagnostics = diagnostics.ToString();
    }
}