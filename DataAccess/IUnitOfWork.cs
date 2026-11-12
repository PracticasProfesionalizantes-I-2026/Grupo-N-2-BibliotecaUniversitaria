namespace BiblioGest.DataAccess;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
