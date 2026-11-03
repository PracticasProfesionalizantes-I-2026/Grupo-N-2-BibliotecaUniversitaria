using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.BusinessLogic.Validation;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class LibroService : ILibroService
{
    private const int TituloMaxLength = 200;
    private const int AutorMaxLength = 150;
    private const int UbicacionMaxLength = 50;

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
        var isbn = ValidarDatos(dto.Titulo, dto.Autor, dto.Isbn, dto.Ubicacion, dto.Stock);
        await ValidarIsbnUnicoAsync(isbn, libroIdActual: null, ct);

        var libro = new Libro
        {
            Titulo = dto.Titulo.Trim(),
            Autor = dto.Autor.Trim(),
            Isbn = isbn,
            Ubicacion = dto.Ubicacion.Trim(),
            Stock = dto.Stock
        };

        var creado = await _libroRepository.CreateAsync(libro, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<LibroResponseDTO> UpdateAsync(Guid id, LibroUpdateDTO dto, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);

        var isbn = ValidarDatos(dto.Titulo, dto.Autor, dto.Isbn, dto.Ubicacion, dto.Stock);
        await ValidarIsbnUnicoAsync(isbn, libroIdActual: id, ct);

        libro.Titulo = dto.Titulo.Trim();
        libro.Autor = dto.Autor.Trim();
        libro.Isbn = isbn;
        libro.Ubicacion = dto.Ubicacion.Trim();
        libro.Stock = dto.Stock;

        await _libroRepository.UpdateAsync(libro, ct);
        return MapToResponseDTO(libro);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);

        if (await _prestamoRepository.TienePrestamosPorLibroAsync(id, ct))
        {
            throw new LibroConHistorialPrestamosException(id);
        }

        await _libroRepository.DeleteAsync(libro, ct);
    }

    private static string ValidarDatos(string titulo, string autor, string isbn, string ubicacion, int stock)
    {
        if (string.IsNullOrWhiteSpace(titulo) ||
            string.IsNullOrWhiteSpace(autor) ||
            string.IsNullOrWhiteSpace(isbn) ||
            string.IsNullOrWhiteSpace(ubicacion))
        {
            throw new LibroInvalidoException(
                "Título, autor, ISBN y ubicación son obligatorios.");
        }

        if (titulo.Trim().Length > TituloMaxLength)
        {
            throw new LibroInvalidoException($"El título no puede superar los {TituloMaxLength} caracteres.");
        }

        if (autor.Trim().Length > AutorMaxLength)
        {
            throw new LibroInvalidoException($"El autor no puede superar los {AutorMaxLength} caracteres.");
        }

        if (ubicacion.Trim().Length > UbicacionMaxLength)
        {
            throw new LibroInvalidoException($"La ubicación no puede superar los {UbicacionMaxLength} caracteres.");
        }

        if (stock < 0)
        {
            throw new LibroInvalidoException("El stock no puede ser negativo.");
        }

        var isbnNormalizado = IsbnValidator.Normalizar(isbn);
        if (!IsbnValidator.EsFormatoValido(isbnNormalizado))
        {
            throw new LibroInvalidoException(
                "El ISBN no es válido. Debe ser un ISBN-10 o ISBN-13 con un dígito verificador correcto.");
        }

        return isbnNormalizado;
    }

    private async Task ValidarIsbnUnicoAsync(string isbnNormalizado, Guid? libroIdActual, CancellationToken ct)
    {
        var existente = await _libroRepository.GetByIsbnAsync(isbnNormalizado, ct);
        if (existente is not null && existente.Id != libroIdActual)
        {
            throw new IsbnDuplicadoException(isbnNormalizado);
        }
    }

    private static LibroResponseDTO MapToResponseDTO(Libro libro) => new()
    {
        Id = libro.Id,
        Titulo = libro.Titulo,
        Autor = libro.Autor,
        Isbn = libro.Isbn,
        Ubicacion = libro.Ubicacion,
        Stock = libro.Stock,
        Disponible = libro.Stock > 0,
        DisponibilidadTexto = libro.Stock switch
        {
            0 => "Sin stock",
            1 => "Disponible (1 ejemplar)",
            _ => $"Disponible ({libro.Stock} ejemplares)"
        }
    };
}
