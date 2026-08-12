using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Auth;

public class AdminUpdateUserRequest
{
    [Required, MaxLength(100)]
    public string NombreCompleto { get; set; } = null!;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int IdRol { get; set; }

    [MinLength(8), MaxLength(100)]
    public string? Password { get; set; }
}
