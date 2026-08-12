namespace SistemaFacturacion.Models;

public class Contrato
{
    public int IdContrato { get; set; }
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string TipoContrato { get; set; } = "Arrendamiento";
    public string Condiciones { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal? MontoMantenimiento { get; set; }
    public decimal? Deposito { get; set; }
    public int DiaPago { get; set; } = 5;
    public bool AplicaITBIS { get; set; }
    public string Estado { get; set; } = "Pendiente";

    public Entidad Entidad { get; set; } = null!;
    public Propiedad? Propiedad { get; set; }
    public Unidad? Unidad { get; set; }
    public ICollection<FacturaCabecera> Facturas { get; set; } = new List<FacturaCabecera>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<DepositoGarantia> Depositos { get; set; } = new List<DepositoGarantia>();
    public ICollection<AcuerdoPago> AcuerdosPago { get; set; } = new List<AcuerdoPago>();
}
