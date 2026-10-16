using System;
using System.Collections.Generic;
using System.Text;
using BiblioGest.Shared.DTOs.Auth;

namespace BiblioGest.BusinessLogic.Interfaces
{
    public interface IAuthService
    {
         Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto , CancellationToken ct = default);
    }
}
