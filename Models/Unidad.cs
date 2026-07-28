namespace SistemaFacturacion.Models;
public class Unidad
{
    public int IdUnidad { get; set; }
    public int IdPropiedad { get; set; }
    public string Codigo { get; set; } = null!;
    public string? Piso { get; set; }
    public decimal MetrosCuadrados { get; set; }
    public string Estado { get; set; } = "Disponible";
    public bool Activo { get; set; } = true;
    public Propiedad Propiedad { get; set; } = null!;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
    public ICollection<FacturaCabecera> Facturas { get; set; } = new List<FacturaCabecera>();
}
