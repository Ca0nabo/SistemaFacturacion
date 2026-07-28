namespace SistemaFacturacion.Models;
public class Role
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Permisos { get; set; }
    public ICollection<User> Usuarios { get; set; } = new List<User>();
}
