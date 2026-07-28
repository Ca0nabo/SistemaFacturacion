namespace SistemaFacturacion.DTOs.Auditoria;
public class AuditoriaLogResponse
{
    public int IdLog { get; set; }
    public int IdUsuario { get; set; }
    public string EmailUsuario { get; set; } = null!;
    public string NombreUsuario { get; set; } = null!;
    public string Accion { get; set; } = null!;
    public string Modulo { get; set; } = null!;
    public int? IdRegistro { get; set; }
    public string? Detalle { get; set; }
    public DateTime FechaRegistro { get; set; }
}
