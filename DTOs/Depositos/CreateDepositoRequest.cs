using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Depositos;

public class CreateDepositoRequest
{
    [Range(1, int.MaxValue)]
    public int IdContrato { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MontoRequerido { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MontoRecibido { get; set; }

    public DateOnly? FechaRecepcion { get; set; }
    public DateOnly? FechaDevolucion { get; set; }

    [Required, MaxLength(20)]
    public string Estado { get; set; } = "Pendiente";

    [MaxLength(30)]
    public string? MetodoPago { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}
