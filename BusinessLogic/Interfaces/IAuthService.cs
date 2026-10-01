using BiblioGest.Shared.DTOs.Auth;

namespace BiblioGest.BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto, CancellationToken ct = default);
}
