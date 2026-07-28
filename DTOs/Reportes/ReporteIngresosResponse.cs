namespace SistemaFacturacion.DTOs.Reportes;
public class ReporteIngresosResponse
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string MesNombre { get; set; } = null!;
    public decimal TotalIngresos { get; set; }
    public int CantidadFacturas { get; set; }
}
public class ReporteFacturacionClienteResponse
{
    public int IdEntidad { get; set; }
    public string RazonSocial { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public decimal TotalFacturado { get; set; }
    public int CantidadFacturas { get; set; }
}
public class ReporteCxCResponse
{
    public decimal TotalPendiente { get; set; }
    public decimal TotalVencido { get; set; }
    public decimal TotalPagado { get; set; }
    public int CantidadPendiente { get; set; }
    public int CantidadVencido { get; set; }
}
