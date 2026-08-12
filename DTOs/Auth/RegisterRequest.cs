using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Auth;
public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = null!;
    [Required, MaxLength(100)]
    public string NombreCompleto { get; set; } = null!;
    [Range(1, int.MaxValue)]
    public int IdRol { get; set; }
}
