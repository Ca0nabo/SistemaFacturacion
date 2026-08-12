using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Acuerdos;

public class PagarCuotaRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    [Required, MaxLength(30)]
    public string MetodoPago { get; set; } = "Transferencia";

    [MaxLength(100)]
    public string? Referencia { get; set; }
}
