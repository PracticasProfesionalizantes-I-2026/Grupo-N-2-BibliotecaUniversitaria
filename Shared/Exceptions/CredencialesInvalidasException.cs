using System;
using System.Collections.Generic;
using System.Text;

namespace BiblioGest.Shared.Exceptions;

public class CredencialesInvalidasException : UnauthorizedException
{
    public CredencialesInvalidasException()  : base("Email o contraseña inválidos.")
    {
    }
}
