namespace BiblioGest.DataAccess;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    // Persiste de una sola vez varios cambios acumulados en memoria sin
    // abrir/cerrar una transacción propia (participa de la transacción
    // ambiente si ya hay una activa en el mismo DbContext).
    Task SaveChangesAsync(CancellationToken ct = default);
}
