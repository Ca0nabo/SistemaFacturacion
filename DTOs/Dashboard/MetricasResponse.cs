namespace SistemaFacturacion.DTOs.Dashboard;

public class MetricasDashboardResponse
{
    public string PeriodoActual { get; set; } = string.Empty;
    public decimal FacturadoMes { get; set; }
    public decimal CobradoMes { get; set; }
    public decimal GastosMes { get; set; }
    public decimal TotalCxC { get; set; }
    public decimal TotalCxP { get; set; }
    public decimal MargenGanancia { get; set; }
    public int FacturasEmitidas { get; set; }
    public int FacturasVencidas { get; set; }
    public int TotalPropiedades { get; set; }
    public int PropiedadesOcupadas { get; set; }
    public int PropiedadesDisponibles { get; set; }
    public int ContratosActivos { get; set; }
    public int ContratosPorVencer { get; set; }
    public decimal TasaOcupacion { get; set; }
    public List<SerieMensualDashboardResponse> TendenciaMensual { get; set; } = new();
    public List<UltimaFacturaDashboardResponse> UltimasFacturas { get; set; } = new();
}

public class SerieMensualDashboardResponse
{
    public string Periodo { get; set; } = string.Empty;
    public decimal Facturado { get; set; }
    public decimal Cobrado { get; set; }
}

public class UltimaFacturaDashboardResponse
{
    public int IdFactura { get; set; }
    public string NumeroECF { get; set; } = string.Empty;
    public string? PeriodoFacturado { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal MontoPendiente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string TipoFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public bool EsPeriodoFuturo { get; set; }
}
