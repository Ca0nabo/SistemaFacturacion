using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Contratos;

public class CreateContratoRequest
{
    [Range(1, int.MaxValue)]
    public int IdEntidad { get; set; }

    [Range(1, int.MaxValue)]
    public int IdPropiedad { get; set; }

    public int? IdUnidad { get; set; }

    [Required, MaxLength(30)]
    public string TipoContrato { get; set; } = "Arrendamiento";

    [Required, MaxLength(500)]
    public string Condiciones { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal MontoAlquilerMensual { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MontoMantenimiento { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DepositoRequerido { get; set; }

    [Range(1, 31)]
    public int DiaPago { get; set; } = 5;

    public bool AplicaITBIS { get; set; }
}
