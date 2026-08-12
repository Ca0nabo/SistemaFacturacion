using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Facturacion;

public class RegistrarPagoRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }

    public DateTime? FechaPago { get; set; }

    [Required, MaxLength(30)]
    public string MetodoPago { get; set; } = "Transferencia";

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }
}
