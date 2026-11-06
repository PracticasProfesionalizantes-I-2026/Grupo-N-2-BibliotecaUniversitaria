namespace BiblioGest.UnitTests;

internal static class IdsDePrueba
{
    private static int _ultimo;

    public static int Nuevo() => Interlocked.Increment(ref _ultimo);
}
