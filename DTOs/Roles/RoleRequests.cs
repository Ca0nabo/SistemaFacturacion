using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Roles;

public class CreateRoleRequest
{
    [Required, MaxLength(50)]
    public string Nombre { get; set; } = null!;

    public List<string> Permisos { get; set; } = [];
}

public class UpdateRoleRequest : CreateRoleRequest { }
