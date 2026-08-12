using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Facturacion;

public class GenerarFacturaMensualRequest
{
    [Range(1, int.MaxValue)]
    public int IdContrato { get; set; }

    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "El período debe tener formato AAAA-MM.")]
    public string Periodo { get; set; } = null!;

    public DateOnly? FechaVencimiento { get; set; }

    [Required, RegularExpression("(?i)^(CONTADO|CREDITO)$", ErrorMessage = "El tipo de factura debe ser CONTADO o CREDITO.")]
    public string TipoFactura { get; set; } = "CONTADO";

    [Range(1, 24)]
    public int CantidadCuotas { get; set; } = 1;
}
