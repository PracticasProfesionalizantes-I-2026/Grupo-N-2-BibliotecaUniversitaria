using System;
using System.Collections.Generic;
using System.Text;

namespace BiblioGest.Shared.DTOs.Auth
{
    public class LoginRequestDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
