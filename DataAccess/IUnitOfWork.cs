using Microsoft.EntityFrameworkCore.Storage;

namespace BiblioGest.DataAccess;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
