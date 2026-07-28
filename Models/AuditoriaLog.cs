namespace SistemaFacturacion.Models;
public class AuditoriaLog
{
    public int IdLog { get; set; }
    public int IdUsuario { get; set; }
    public string Accion { get; set; } = null!;
    public string Modulo { get; set; } = null!;
    public int? IdRegistro { get; set; }
    public string? Detalle { get; set; }
    public DateTime FechaRegistro { get; set; }
    public User Usuario { get; set; } = null!;
}
