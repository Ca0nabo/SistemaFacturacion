namespace SistemaFacturacion.Models;
public class Propiedad
{
    public int IdPropiedad { get; set; }
    public int IdEntidad { get; set; }
    public string TipoPropiedad { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public string? Sector { get; set; }
    public string? Ciudad { get; set; }
    public decimal MetrosCuadrados { get; set; }
    public int? CantidadHabitaciones { get; set; }
    public int? CantidadBanos { get; set; }
    public bool TieneParqueo { get; set; }
    public string Estado { get; set; } = "Disponible";
    public bool Activo { get; set; } = true;
    public Entidad Entidad { get; set; } = null!;
    public ICollection<Unidad> Unidades { get; set; } = new List<Unidad>();
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
    public ICollection<FacturaCabecera> Facturas { get; set; } = new List<FacturaCabecera>();
}
