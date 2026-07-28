namespace SistemaFacturacion.Models;
public class ParametrosEmpresa
{
    public int IdParametro { get; set; }
    public string NombreEmpresa { get; set; } = null!;
    public string SecuenciaEmpresa { get; set; } = null!;
    public string SecuenciaFiscalECF { get; set; } = null!;
    public decimal PorcentajeITBIS { get; set; } = 0.18m;
}
