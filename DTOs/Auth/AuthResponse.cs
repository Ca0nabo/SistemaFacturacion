namespace SistemaFacturacion.DTOs.Auth;
public class AuthResponse
{
    public int IdUsuario { get; set; }
    public string Email { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public string Token { get; set; } = null!;
}
