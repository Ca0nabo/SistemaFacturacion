namespace SistemaFacturacion.DTOs.Entidades;
public class EntidadResponse
{
    public int IdEntidad { get; set; }
    public string Tipo { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public bool Activo { get; set; }
}
