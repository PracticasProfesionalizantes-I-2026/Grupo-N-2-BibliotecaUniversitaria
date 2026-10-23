using BiblioGest.BusinessLogic.Mapping;
using BiblioGest.DataAccess.Entities;
using Xunit;

namespace BiblioGest.UnitTests;

public class PrestamoMapperTests
{
    private static readonly DateTime Ahora = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Prestamo NuevoPrestamo(DateTime fechaVencimiento, EstadoPrestamo estado, DateTime? fechaDevolucion = null) => new()
    {
        Id = Guid.NewGuid(),
        LectorId = Guid.NewGuid(),
        LibroId = Guid.NewGuid(),
        FechaPrestamo = fechaVencimiento.AddDays(-14),
        FechaVencimiento = fechaVencimiento,
        FechaDevolucion = fechaDevolucion,
        Estado = estado
    };

    [Fact]
    public void ToResponseDTO_PrestamoActivoNoVencido_CalculaDiasRestantesYMensaje()
    {
        var prestamo = NuevoPrestamo(Ahora.AddDays(3), EstadoPrestamo.Activo);

        var dto = PrestamoMapper.ToResponseDTO(prestamo, Ahora);

        Assert.Equal("Activo", dto.Estado);
        Assert.Equal(3, dto.DiasRestantes);
        Assert.Equal(0, dto.DiasDeAtraso);
        Assert.False(dto.EnMora);
        Assert.Equal("Vence en 3 días.", dto.Mensaje);
    }

    [Fact]
    public void ToResponseDTO_PrestamoQueVenceHoy_DevuelveMensajeVenceHoy()
    {
        var prestamo = NuevoPrestamo(Ahora, EstadoPrestamo.Activo);

        var dto = PrestamoMapper.ToResponseDTO(prestamo, Ahora);

        Assert.Equal("Activo", dto.Estado);
        Assert.Equal(0, dto.DiasRestantes);
        Assert.Equal("Vence hoy.", dto.Mensaje);
    }

    [Fact]
    public void ToResponseDTO_PrestamoVencido_CalculaDiasDeAtrasoYEstadoVencido()
    {
        var prestamo = NuevoPrestamo(Ahora.AddDays(-5), EstadoPrestamo.Activo);

        var dto = PrestamoMapper.ToResponseDTO(prestamo, Ahora);

        Assert.Equal("Vencido", dto.Estado);
        Assert.Equal(0, dto.DiasRestantes);
        Assert.Equal(5, dto.DiasDeAtraso);
        Assert.True(dto.EnMora);
        Assert.Equal("Atrasado 5 días.", dto.Mensaje);
    }

    [Fact]
    public void ToResponseDTO_PrestamoDevueltoATiempo_EstadoDevueltoSinAtraso()
    {
        var prestamo = NuevoPrestamo(Ahora.AddDays(2), EstadoPrestamo.Devuelto, fechaDevolucion: Ahora.AddDays(-1));

        var dto = PrestamoMapper.ToResponseDTO(prestamo, Ahora);

        Assert.Equal("Devuelto", dto.Estado);
        Assert.Null(dto.DiasRestantes);
        Assert.Equal(0, dto.DiasDeAtraso);
        Assert.False(dto.EnMora);
        Assert.Equal("Devuelto a tiempo.", dto.Mensaje);
    }

    [Fact]
    public void ToResponseDTO_PrestamoDevueltoConAtraso_CalculaDiasDeAtraso()
    {
        var vencimiento = Ahora.AddDays(-10);
        var prestamo = NuevoPrestamo(vencimiento, EstadoPrestamo.Devuelto, fechaDevolucion: vencimiento.AddDays(4));

        var dto = PrestamoMapper.ToResponseDTO(prestamo, Ahora);

        Assert.Equal("Devuelto con atraso", dto.Estado);
        Assert.Null(dto.DiasRestantes);
        Assert.Equal(4, dto.DiasDeAtraso);
        Assert.Equal("Devuelto con 4 días de atraso.", dto.Mensaje);
    }

    [Fact]
    public void ToResponseDTO_UnDiaDeDiferencia_UsaSingularEnElMensaje()
    {
        var prestamo = NuevoPrestamo(Ahora.AddDays(1), EstadoPrestamo.Activo);

        var dto = PrestamoMapper.ToResponseDTO(prestamo, Ahora);

        Assert.Equal(1, dto.DiasRestantes);
        Assert.Equal("Vence en 1 día.", dto.Mensaje);
    }
}
