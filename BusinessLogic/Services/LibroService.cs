using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class LibroService : ILibroService
{
    private readonly ILibroRepository _libroRepository;
    private readonly IPrestamoRepository _prestamoRepository;

    public LibroService(ILibroRepository libroRepository, IPrestamoRepository prestamoRepository)
    {
        _libroRepository = libroRepository;
        _prestamoRepository = prestamoRepository;
    }

    public async Task<IReadOnlyList<LibroResponseDTO>> GetAllAsync(string? busqueda, CancellationToken ct = default)
    {
        var libros = await _libroRepository.GetAllAsync(busqueda, ct);
        return libros.Select(MapToResponseDTO).ToList();
    }

    public async Task<LibroResponseDTO> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);
        return MapToResponseDTO(libro);
    }

    public async Task<LibroResponseDTO> CreateAsync(LibroCreateDTO dto, CancellationToken ct = default)
    {
        ValidarDatos(dto.Titulo, dto.Autor, dto.Isbn, dto.Ubicacion, dto.Stock);

        var libro = new Libro
        {
            Titulo = dto.Titulo.Trim(),
            Autor = dto.Autor.Trim(),
            Isbn = dto.Isbn.Trim(),
            Ubicacion = dto.Ubicacion.Trim(),
            Stock = dto.Stock
        };

        var creado = await _libroRepository.CreateAsync(libro, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<LibroResponseDTO> UpdateAsync(Guid id, LibroUpdateDTO dto, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);

        ValidarDatos(dto.Titulo, dto.Autor, dto.Isbn, dto.Ubicacion, dto.Stock);

        libro.Titulo = dto.Titulo.Trim();
        libro.Autor = dto.Autor.Trim();
        libro.Isbn = dto.Isbn.Trim();
        libro.Ubicacion = dto.Ubicacion.Trim();
        libro.Stock = dto.Stock;

        await _libroRepository.UpdateAsync(libro, ct);
        return MapToResponseDTO(libro);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);

        if (await _prestamoRepository.TieneActivosPorLibroAsync(id, ct))
        {
            throw new LibroConPrestamosActivosException(id);
        }

        await _libroRepository.DeleteAsync(libro, ct);
    }

    private static void ValidarDatos(string titulo, string autor, string isbn, string ubicacion, int stock)
    {
        if (string.IsNullOrWhiteSpace(titulo) ||
            string.IsNullOrWhiteSpace(autor) ||
            string.IsNullOrWhiteSpace(isbn) ||
            string.IsNullOrWhiteSpace(ubicacion))
        {
            throw new LibroInvalidoException(
                "Título, autor, ISBN y ubicación son obligatorios.");
        }

        if (stock < 0)
        {
            throw new LibroInvalidoException("El stock no puede ser negativo.");
        }
    }

    private static LibroResponseDTO MapToResponseDTO(Libro libro) => new()
    {
        Id = libro.Id,
        Titulo = libro.Titulo,
        Autor = libro.Autor,
        Isbn = libro.Isbn,
        Ubicacion = libro.Ubicacion,
        Stock = libro.Stock
    };
}
