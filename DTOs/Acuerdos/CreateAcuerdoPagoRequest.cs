using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Acuerdos;

public class CreateAcuerdoPagoRequest
{
    [Range(1, int.MaxValue)]
    public int IdContrato { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int? IdFacturaOrigen { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal MontoOriginal { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal MontoAcordado { get; set; }

    [Range(2, 120)]
    public int CantidadCuotas { get; set; }

    public DateOnly FechaInicio { get; set; }

    [Range(1, 31)]
    public int DiaPago { get; set; } = 5;

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}
