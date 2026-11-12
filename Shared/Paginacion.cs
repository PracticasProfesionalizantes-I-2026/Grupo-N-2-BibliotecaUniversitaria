namespace BiblioGest.Shared;

public static class Paginacion
{
    public const int PageSizePorDefecto = 20;
    public const int PageSizeMaximo = 100;

    public static int NormalizarPageNumber(int? pageNumber) =>
        pageNumber is > 0 ? pageNumber.Value : 1;

    public static int NormalizarPageSize(int? pageSize) => pageSize switch
    {
        null or <= 0 => PageSizePorDefecto,
        > PageSizeMaximo => PageSizeMaximo,
        _ => pageSize.Value
    };
}
