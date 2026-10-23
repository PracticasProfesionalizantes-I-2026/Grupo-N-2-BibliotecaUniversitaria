namespace BiblioGest.UnitTests;

// TimeProvider fijo para poder testear determinísticamente los cálculos de
// fechas (días restantes, atraso, estado) sin depender del reloj real.
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _ahora;

    public FixedTimeProvider(DateTimeOffset ahora) => _ahora = ahora;

    public override DateTimeOffset GetUtcNow() => _ahora;
}
