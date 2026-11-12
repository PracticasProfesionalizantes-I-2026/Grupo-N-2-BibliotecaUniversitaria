using BiblioGest.BusinessLogic.Interfaces;
using BiblioGest.DataAccess;
using BiblioGest.DataAccess.Entities;
using BiblioGest.DataAccess.Repositories;

namespace BiblioGest.BusinessLogic.Services;

public class ColaDeReservas : IColaDeReservas
{
    public const int HorasParaRetirar = 24;

    private readonly IReservaRepository _reservaRepository;
    private readonly ILibroRepository _libroRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ColaDeReservas(IReservaRepository reservaRepository, ILibroRepository libroRepository, IUnitOfWork unitOfWork)
    {
        _reservaRepository = reservaRepository;
        _libroRepository = libroRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ProcesarAsignacionesVencidasAsync(Libro libro, CancellationToken ct = default)
    {
        var activas = (await _reservaRepository.GetActivasPorLibroAsync(libro.Id, ct)).ToList();
        var ahora = DateTime.UtcNow;
        var huboCambios = false;

        var vencida = BuscarAsignacionVencida(activas, ahora);
        while (vencida is not null)
        {
            vencida.Estado = EstadoReserva.Vencida;
            await _reservaRepository.UpdateAsync(vencida, ct, guardarCambios: false);
            activas.Remove(vencida);
            huboCambios = true;

            await LiberarEjemplarAsync(libro, activas, vencida.FechaLimiteRetiro!.Value, guardarCambios: false, ct);
            vencida = BuscarAsignacionVencida(activas, ahora);
        }

        if (huboCambios)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> AsignarAlSiguienteAsync(Libro libro, DateTime desde, CancellationToken ct = default, bool guardarCambios = true)
    {
        var activas = (await _reservaRepository.GetActivasPorLibroAsync(libro.Id, ct)).ToList();
        return await AsignarAlSiguienteAsync(activas, desde, guardarCambios, ct);
    }

    public async Task LiberarEjemplarAsync(Libro libro, DateTime desde, CancellationToken ct = default)
    {
        var activas = (await _reservaRepository.GetActivasPorLibroAsync(libro.Id, ct)).ToList();
        await LiberarEjemplarAsync(libro, activas, desde, guardarCambios: true, ct);
    }

    private async Task LiberarEjemplarAsync(Libro libro, List<Reserva> activas, DateTime desde, bool guardarCambios, CancellationToken ct)
    {
        if (await AsignarAlSiguienteAsync(activas, desde, guardarCambios, ct))
        {
            return;
        }

        libro.Stock += 1;
        await _libroRepository.UpdateAsync(libro, ct, guardarCambios);
    }

    private async Task<bool> AsignarAlSiguienteAsync(List<Reserva> activas, DateTime desde, bool guardarCambios, CancellationToken ct)
    {
        var siguiente = activas.FirstOrDefault(r => r.Estado == EstadoReserva.EnCola);
        if (siguiente is null)
        {
            return false;
        }

        siguiente.Estado = EstadoReserva.Asignada;
        siguiente.FechaAsignacion = desde;
        siguiente.FechaLimiteRetiro = desde.AddHours(HorasParaRetirar);
        await _reservaRepository.UpdateAsync(siguiente, ct, guardarCambios);
        return true;
    }

    private static Reserva? BuscarAsignacionVencida(List<Reserva> activas, DateTime ahora) =>
        activas.FirstOrDefault(r =>
            r.Estado == EstadoReserva.Asignada &&
            r.FechaLimiteRetiro.HasValue &&
            r.FechaLimiteRetiro.Value <= ahora);
}
