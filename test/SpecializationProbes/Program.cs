namespace SpecializationProbes;

internal static class Program
{
    private static void Main()
    {
        OverloadBinding.Run();
        Escape.Run();
        IndirectionCost.Run();
    }
}
