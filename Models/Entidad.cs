namespace SistemaFacturacion.Models;
public class Entidad
{
    public int IdEntidad { get; set; }
    public string Tipo { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public ICollection<FacturaCabecera> Facturas { get; set; } = new List<FacturaCabecera>();
}
