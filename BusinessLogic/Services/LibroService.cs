using System.Text.RegularExpressions;
using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;
using BiblioGest.Shared.DTOs.Libros;
using BiblioGest.Shared.Exceptions;

namespace BiblioGest.BusinessLogic.Services;

public class LibroService : ILibroService
{
    private const int StockMaximo = 10;
    private const int LargoMaximoTexto = 150;
    private static readonly Regex IsbnRegex = new(@"^(\d{9}[\dX]|\d{13})$");
    private static readonly Regex UbicacionRegex = new(@"^[A-J]([1-9]|[12][0-9]|30)-[1-6]$");

    private readonly ILibroRepository _libroRepository;
    private readonly IPrestamoRepository _prestamoRepository;
    private readonly IReservaRepository _reservaRepository;

    public LibroService(ILibroRepository libroRepository, IPrestamoRepository prestamoRepository, IReservaRepository reservaRepository)
    {
        _libroRepository = libroRepository;
        _prestamoRepository = prestamoRepository;
        _reservaRepository = reservaRepository;
    }

    public async Task<IReadOnlyList<LibroResponseDTO>> GetAllAsync(string? busqueda, CancellationToken ct = default)
    {
        var libros = await _libroRepository.GetAllAsync(busqueda, ct);
        return libros.Select(MapToResponseDTO).ToList();
    }

    public async Task<LibroResponseDTO> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);
        return MapToResponseDTO(libro);
    }

    public async Task<LibroResponseDTO> CreateAsync(LibroCreateDTO dto, CancellationToken ct = default)
    {
        ValidarDatos(dto.Titulo, dto.Autor, dto.Isbn, dto.Ubicacion, dto.Stock);
        await ValidarIsbnUnicoAsync(dto.Isbn, libroIdActual: null, ct);

        var libro = new Libro
        {
            Titulo = dto.Titulo.Trim(),
            Autor = dto.Autor.Trim(),
            Isbn = dto.Isbn.Trim(),
            Ubicacion = dto.Ubicacion.Trim().ToUpperInvariant(),
            Stock = dto.Stock
        };

        var creado = await _libroRepository.CreateAsync(libro, ct);
        return MapToResponseDTO(creado);
    }

    public async Task<LibroResponseDTO> UpdateAsync(int id, LibroUpdateDTO dto, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);

        ValidarDatos(dto.Titulo, dto.Autor, dto.Isbn, dto.Ubicacion, dto.Stock);
        await ValidarIsbnUnicoAsync(dto.Isbn, libroIdActual: id, ct);

        libro.Titulo = dto.Titulo.Trim();
        libro.Autor = dto.Autor.Trim();
        libro.Isbn = dto.Isbn.Trim();
        libro.Ubicacion = dto.Ubicacion.Trim().ToUpperInvariant();
        libro.Stock = dto.Stock;

        await _libroRepository.UpdateAsync(libro, ct);
        return MapToResponseDTO(libro);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var libro = await _libroRepository.GetByIdAsync(id, ct) ?? throw new LibroNotFoundException(id);

        if (await _prestamoRepository.TieneActivosPorLibroAsync(id, ct))
        {
            throw new LibroConPrestamosActivosException(id);
        }

        if (await _reservaRepository.TieneActivasPorLibroAsync(id, ct))
        {
            throw new LibroConReservasActivasException(id);
        }

        if (await _prestamoRepository.TienePrestamosPorLibroAsync(id, ct))
        {
            throw new LibroConHistorialException(id);
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

        if (titulo.Trim().Length > LargoMaximoTexto || autor.Trim().Length > LargoMaximoTexto)
        {
            throw new LibroInvalidoException(
                $"El título y el autor no pueden superar los {LargoMaximoTexto} caracteres.");
        }

        if (stock < 0)
        {
            throw new LibroInvalidoException("El stock no puede ser negativo.");
        }

        if (stock > StockMaximo)
        {
            throw new StockExcedidoException(StockMaximo);
        }

        if (!IsbnRegex.IsMatch(NormalizarIsbn(isbn)))
        {
            throw new LibroInvalidoException(
                "El ISBN debe tener 10 o 13 dígitos (se permiten guiones; el ISBN-10 puede terminar en X).");
        }

        if (!UbicacionRegex.IsMatch(ubicacion.Trim().ToUpperInvariant()))
        {
            throw new LibroInvalidoException(
                "La ubicación debe tener el formato Pasillo-Estantería-Estante, por ejemplo A1-4: " +
                "pasillo de A a J, estantería de 1 a 30 y estante de 1 a 6.");
        }
    }

    private async Task ValidarIsbnUnicoAsync(string isbn, int? libroIdActual, CancellationToken ct)
    {
        if (await _libroRepository.ExisteIsbnAsync(NormalizarIsbn(isbn), libroIdActual, ct))
        {
            throw new IsbnDuplicadoException(isbn.Trim());
        }
    }

    private static string NormalizarIsbn(string isbn) => isbn.Trim().Replace("-", "").ToUpperInvariant();

    private static LibroResponseDTO MapToResponseDTO(Libro libro) => new()
    {
        Id = libro.Id,
        Titulo = libro.Titulo,
        Autor = libro.Autor,
        Isbn = libro.Isbn,
        Ubicacion = libro.Ubicacion,
        Stock = libro.Stock,
        Disponible = libro.Stock > 0
    };
}
