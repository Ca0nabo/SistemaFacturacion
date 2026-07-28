using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.Models;
public class User
{
    public int IdUsuario { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public int IdRol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public Role Rol { get; set; } = null!;
}
