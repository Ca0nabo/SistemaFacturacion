namespace SistemaFacturacion.DTOs.Unidades;
public class UnidadResponse
{
    public int IdUnidad { get; set; }
    public int IdPropiedad { get; set; }
    public string DireccionPropiedad { get; set; } = null!;
    public string Codigo { get; set; } = null!;
    public string? Piso { get; set; }
    public decimal MetrosCuadrados { get; set; }
    public string Estado { get; set; } = null!;
    public bool Activo { get; set; }
}
