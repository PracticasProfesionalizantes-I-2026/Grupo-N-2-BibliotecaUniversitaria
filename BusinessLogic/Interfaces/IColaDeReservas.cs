using BiblioGest.DataAccess.Entities;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IColaDeReservas
{
    Task ProcesarAsignacionesVencidasAsync(Libro libro, CancellationToken ct = default);
    Task<bool> AsignarAlSiguienteAsync(Libro libro, DateTime desde, CancellationToken ct = default, bool guardarCambios = true);
    Task LiberarEjemplarAsync(Libro libro, DateTime desde, CancellationToken ct = default);
}
