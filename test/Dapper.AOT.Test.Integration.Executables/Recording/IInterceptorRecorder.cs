namespace Dapper.AOT.Test.Integration.Executables.Recording;

public interface IInterceptorRecorder
{
    /// <summary>
    /// returns true if expected interception was called
    /// </summary>
    public bool WasCalled { get; }
    
    /// <summary>
    /// Returns diagnostics of recording
    /// </summary>
    public string? Diagnostics { get; }
    
    /// <summary>
    /// Is executed in the interception
    /// </summary>
    public void Record();
}