using System;
using System.Collections.Generic;
using System.Text;

namespace BiblioGest.Shared.Exceptions
{
    public abstract class UnauthorizedException : Exception
    {
        protected UnauthorizedException(string message) : base(message)
        {
        }
    }
}
