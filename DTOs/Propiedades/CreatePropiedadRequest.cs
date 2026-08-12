using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacion.DTOs.Propiedades;

public class CreatePropiedadRequest
{
    [Range(1, int.MaxValue)]
    public int IdEntidad { get; set; }

    [Required, MaxLength(30)]
    public string Codigo { get; set; } = null!;

    [Required, MaxLength(30)]
    public string TipoPropiedad { get; set; } = null!;

    [Required, MaxLength(300)]
    public string Direccion { get; set; } = null!;

    [MaxLength(100)]
    public string? Sector { get; set; }

    [MaxLength(100)]
    public string? Ciudad { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal MetrosCuadrados { get; set; }

    public int? CantidadHabitaciones { get; set; }
    public int? CantidadBanos { get; set; }
    public bool TieneParqueo { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CanonMensualSugerido { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MantenimientoMensualSugerido { get; set; }
}
