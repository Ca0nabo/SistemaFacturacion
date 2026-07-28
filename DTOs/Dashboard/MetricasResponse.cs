namespace SistemaFacturacion.DTOs.Dashboard;
public class MetricasDashboardResponse
{
    public decimal IngresosMes { get; set; }
    public decimal GastosMes { get; set; }
    public decimal TotalCxC { get; set; }
    public decimal TotalCxP { get; set; }
    public decimal MargenGanancia { get; set; }
    public int FacturasEmitidas { get; set; }
    public int MovimientosVencidos { get; set; }
    public int TotalPropiedades { get; set; }
    public int PropiedadesOcupadas { get; set; }
    public int PropiedadesDisponibles { get; set; }
    public int ContratosActivos { get; set; }
    public int ContratosPorVencer { get; set; }
    public decimal TasaOcupacion { get; set; }
}
