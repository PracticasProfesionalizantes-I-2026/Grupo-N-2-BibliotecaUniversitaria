namespace BiblioGest.BusinessLogic.Validation;

public static class IsbnValidator
{
    // Quita guiones y espacios, que se permiten al ingresar el ISBN pero no se guardan.
    public static string Normalizar(string isbn) => isbn.Replace("-", "").Replace(" ", "").Trim();

    public static bool EsFormatoValido(string isbnNormalizado) =>
        isbnNormalizado.Length switch
        {
            10 => EsIsbn10Valido(isbnNormalizado),
            13 => EsIsbn13Valido(isbnNormalizado),
            _ => false
        };

    private static bool EsIsbn10Valido(string isbn)
    {
        var suma = 0;
        for (var i = 0; i < 10; i++)
        {
            int digito;
            if (i == 9 && (isbn[i] == 'X' || isbn[i] == 'x'))
            {
                digito = 10;
            }
            else if (char.IsDigit(isbn[i]))
            {
                digito = isbn[i] - '0';
            }
            else
            {
                return false;
            }

            suma += digito * (10 - i);
        }

        return suma % 11 == 0;
    }

    private static bool EsIsbn13Valido(string isbn)
    {
        var suma = 0;
        for (var i = 0; i < 13; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }

            var digito = isbn[i] - '0';
            suma += digito * (i % 2 == 0 ? 1 : 3);
        }

        return suma % 10 == 0;
    }
}
