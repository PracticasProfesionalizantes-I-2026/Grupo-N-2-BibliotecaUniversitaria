using System;
using System.Collections.Generic;
using System.Text;

namespace BiblioGest.Shared.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public  string Rol { get; set; } = string.Empty;
    }
}
