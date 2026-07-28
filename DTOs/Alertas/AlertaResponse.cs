namespace SistemaFacturacion.DTOs.Alertas;
public class AlertaResponse
{
    public int Id { get; set; }
    public string Tipo { get; set; } = null!;
    public string Referencia { get; set; } = null!;
    public string Entidad { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Estado { get; set; } = null!;
    public DateOnly FechaVencimiento { get; set; }
    public string Criticidad { get; set; } = null!;
}
