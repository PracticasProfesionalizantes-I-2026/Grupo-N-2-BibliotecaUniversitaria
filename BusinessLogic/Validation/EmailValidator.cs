using System.Text.RegularExpressions;

namespace BiblioGest.BusinessLogic.Validation;

public static partial class EmailValidator
{
    public static bool EsFormatoValido(string email) => EmailRegex().IsMatch(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
