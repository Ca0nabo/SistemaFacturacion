namespace SistemaFacturacion.DTOs.Propiedades;
public class PropiedadResponse
{
    public int IdPropiedad { get; set; }
    public int IdEntidad { get; set; }
    public string RazonSocialPropietario { get; set; } = null!;
    public string RncCedulaPropietario { get; set; } = null!;
    public string TipoPropiedad { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public string? Sector { get; set; }
    public string? Ciudad { get; set; }
    public decimal MetrosCuadrados { get; set; }
    public int? CantidadHabitaciones { get; set; }
    public int? CantidadBanos { get; set; }
    public bool TieneParqueo { get; set; }
    public string Estado { get; set; } = null!;
    public bool Activo { get; set; }
    public int CantidadUnidades { get; set; }
    public int CantidadUnidadesOcupadas { get; set; }
}
