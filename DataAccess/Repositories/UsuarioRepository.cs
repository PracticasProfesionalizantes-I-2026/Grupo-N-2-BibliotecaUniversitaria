using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly BiblioGestDbContext _context;

    public UsuarioRepository(BiblioGestDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Usuarios.AsNoTracking().OrderBy(u => u.Nombre).ToListAsync(ct);

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<Usuario> CreateAsync(Usuario usuario, CancellationToken ct = default)
    {
        usuario.Id = Guid.NewGuid();
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(ct);
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken ct = default)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync(ct);
    }
}
