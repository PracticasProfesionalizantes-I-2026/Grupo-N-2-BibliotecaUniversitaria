namespace BiblioGest.Shared.DTOs.Reportes;

public class ReporteResumenDTO
{
    public int TotalLibros { get; set; }
    public int LibrosSinStock { get; set; }
    public int TotalLectores { get; set; }
    public int PrestamosActivos { get; set; }
    public int PrestamosDevueltos { get; set; }
    public int PrestamosVencidos { get; set; }
}
