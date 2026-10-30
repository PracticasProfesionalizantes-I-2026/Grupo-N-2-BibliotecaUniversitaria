using Microsoft.EntityFrameworkCore.Storage;

namespace BiblioGest.DataAccess;

public class UnitOfWork : IUnitOfWork
{
    private readonly BiblioGestDbContext _context;

    public UnitOfWork(BiblioGestDbContext context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        _context.Database.BeginTransactionAsync(ct);
}
