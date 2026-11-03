namespace BiblioGest.UnitTests;

// Genera ISBN-13 válidos (con dígito verificador correcto) para usar en tests,
// ya que LibroService ahora valida el formato antes de guardar.
internal static class IsbnTestHelper
{
    public static string NuevoIsbnValido()
    {
        var digitos = new int[12];
        digitos[0] = 9;
        digitos[1] = 7;
        digitos[2] = 8;
        for (var i = 3; i < 12; i++)
        {
            digitos[i] = Random.Shared.Next(0, 10);
        }

        var suma = 0;
        for (var i = 0; i < 12; i++)
        {
            suma += digitos[i] * (i % 2 == 0 ? 1 : 3);
        }

        var digitoVerificador = (10 - suma % 10) % 10;

        return string.Concat(digitos.Select(d => d.ToString())) + digitoVerificador;
    }
}
