using BiblioGest.DataAccess.Entities;
namespace BiblioGest.DataAccess.Repositories
{
    public interface IUsuarioRepository
    {
        Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct = default);
        Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<Usuario> CreateAsync(Usuario usuario, CancellationToken ct = default);
        Task UpdateAsync(Usuario usuario, CancellationToken ct = default);
        Task DeleteAsync(Usuario usuario, CancellationToken ct = default);
    }
}
