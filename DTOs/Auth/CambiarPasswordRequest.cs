using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Auth;
public class CambiarPasswordRequest
{
    [Required, MinLength(6), MaxLength(100)]
    public string PasswordActual { get; set; } = null!;
    [Required, MinLength(6), MaxLength(100)]
    public string NuevaPassword { get; set; } = null!;
}
