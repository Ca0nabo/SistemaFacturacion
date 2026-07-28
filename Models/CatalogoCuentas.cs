namespace SistemaFacturacion.Models;
public class CatalogoCuentas
{
    public int IdCuentaContable { get; set; }
    public string NombreCuenta { get; set; } = null!;
    public ICollection<AsientoContable> Asientos { get; set; } = new List<AsientoContable>();
}
