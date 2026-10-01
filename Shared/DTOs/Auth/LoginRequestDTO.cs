using System.ComponentModel.DataAnnotations;

namespace BiblioGest.Shared.DTOs.Auth;

public class LoginRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
