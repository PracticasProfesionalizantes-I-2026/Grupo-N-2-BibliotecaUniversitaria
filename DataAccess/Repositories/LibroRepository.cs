using System.Security.Cryptography;
using BiblioGest.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BiblioGest.DataAccess.Repositories;

public class LibroRepository : ILibroRepository
{
    private readonly BiblioGestDbContext _context;

    public LibroRepository(BiblioGestDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Libro>> GetAllAsync(string? busqueda, CancellationToken ct = default)
    {
        var query = _context.Libros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(l =>
                EF.Functions.Like(l.Titulo, $"%{busqueda}%") ||
                EF.Functions.Like(l.Autor, $"%{busqueda}%") ||
                EF.Functions.Like(l.Isbn, $"%{busqueda}%"));
        }

        return await query.OrderBy(l => l.Titulo).ToListAsync(ct);
    }

    public Task<Libro?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Libros.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<bool> ExisteIsbnAsync(string isbnSinGuiones, int? excluirLibroId, CancellationToken ct = default) =>
        _context.Libros.AsNoTracking().AnyAsync(l =>
            l.Isbn.Replace("-", "").ToUpper() == isbnSinGuiones &&
            (excluirLibroId == null || l.Id != excluirLibroId), ct);

    public async Task<Libro> CreateAsync(Libro libro, CancellationToken ct = default)
    {
        _context.Libros.Add(libro);
        await _context.SaveChangesAsync(ct);
        return libro;
    }

    public async Task UpdateAsync(Libro libro, CancellationToken ct = default, bool guardarCambios = true)
    {
        // RowVersion es el token de concurrencia optimista: el valor que trae
        // "libro" es el que se leyó antes de modificarlo (el original real),
        // y acá se genera uno nuevo para guardar. Si otra operación ya lo
        // cambió entre medio, el UPDATE no afecta filas y EF tira
        // DbUpdateConcurrencyException.
        var rowVersionLeido = libro.RowVersion;
        var entry = _context.Libros.Update(libro);
        entry.Property(l => l.RowVersion).OriginalValue = rowVersionLeido;
        entry.Property(l => l.RowVersion).CurrentValue = RandomNumberGenerator.GetBytes(8);

        if (guardarCambios)
        {
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Libro libro, CancellationToken ct = default)
    {
        _context.Libros.Remove(libro);
        await _context.SaveChangesAsync(ct);
    }
}
