using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Facturacion;

public class CreateFacturaRequest
{
    [Range(1, int.MaxValue)]
    public int IdEntidad { get; set; }

    public int? IdContrato { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public bool AplicaITBIS { get; set; }

    [Required, RegularExpression("(?i)^(CONTADO|CREDITO)$", ErrorMessage = "El tipo de factura debe ser CONTADO o CREDITO.")]
    public string TipoFactura { get; set; } = "CONTADO";

    [Range(1, 24)]
    public int CantidadCuotas { get; set; } = 1;

    [Required, MinLength(1)]
    public List<FacturaDetalleItem> Detalles { get; set; } = new();
}
