using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Auth;
public class UpdateUserRequest
{
    [Required, MaxLength(100)]
    public string NombreCompleto { get; set; } = null!;
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;
}
