namespace Dapper.AOT.Test.Integration.Executables.Recording;

public static class InterceptorRecorderResolver
{
    static IInterceptorRecorder? _recorder;

    public static IInterceptorRecorder? Resolve() => _recorder;
    public static void Register(IInterceptorRecorder? recorder) => _recorder = recorder;
}