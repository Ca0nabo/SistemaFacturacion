using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Dashboard;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context) => _context = context;

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        // El dashboard representa el mes local del equipo donde se ejecuta la aplicación.
        // Las fechas de pagos/facturas se guardan en UTC, por eso los límites se convierten a UTC.
        var ahoraLocal = DateTime.Now;
        var periodoActual = $"{ahoraLocal.Year:D4}-{ahoraLocal.Month:D2}";
        var inicioMesLocal = new DateTime(ahoraLocal.Year, ahoraLocal.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var inicioMesSiguienteLocal = inicioMesLocal.AddMonths(1);
        var inicioMesUtc = inicioMesLocal.ToUniversalTime();
        var inicioMesSiguienteUtc = inicioMesSiguienteLocal.ToUniversalTime();
        var hoyDate = DateOnly.FromDateTime(ahoraLocal);

        // Facturación mensual: se contabiliza por el período cobrado, no por el día en que se creó la factura.
        // Las facturas manuales se contabilizan por su fecha de emisión.
        var facturadoMes = await _context.FacturasCabecera
            .Where(f => f.Estado != "ANULADA" &&
                ((f.OrigenFactura == "AlquilerMensual" && f.PeriodoFacturado == periodoActual) ||
                 (f.OrigenFactura != "AlquilerMensual" &&
                  f.FechaEmision >= inicioMesUtc && f.FechaEmision < inicioMesSiguienteUtc)))
            .SumAsync(f => (decimal?)f.Total) ?? 0m;

        // Lo cobrado se calcula por la fecha real del pago.
        var cobradoMes = await _context.Pagos
            .Where(p => p.FechaPago >= inicioMesUtc && p.FechaPago < inicioMesSiguienteUtc)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        var inicioMesDate = new DateOnly(ahoraLocal.Year, ahoraLocal.Month, 1);
        var inicioMesSiguienteDate = inicioMesDate.AddMonths(1);
        var gastosMes = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxP" &&
                        m.FechaVencimiento >= inicioMesDate &&
                        m.FechaVencimiento < inicioMesSiguienteDate)
            .SumAsync(m => (decimal?)m.MontoOriginal) ?? 0m;

        // Las facturas generadas por error para meses futuros no deben inflar las cuentas por cobrar actuales.
        var cxcPendientes = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxC" && m.MontoPendiente > 0 && m.Factura.Estado != "ANULADA")
            .Select(m => new
            {
                m.MontoPendiente,
                m.Factura.PeriodoFacturado
            })
            .ToListAsync();

        var totalCxC = cxcPendientes
            .Where(m => string.IsNullOrWhiteSpace(m.PeriodoFacturado) ||
                        string.CompareOrdinal(m.PeriodoFacturado, periodoActual) <= 0)
            .Sum(m => m.MontoPendiente);

        var totalCxP = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxP" && m.MontoPendiente > 0)
            .SumAsync(m => (decimal?)m.MontoPendiente) ?? 0m;

        var facturasEmitidas = await _context.FacturasCabecera
            .CountAsync(f => f.Estado != "ANULADA" &&
                ((f.OrigenFactura == "AlquilerMensual" && f.PeriodoFacturado == periodoActual) ||
                 (f.OrigenFactura != "AlquilerMensual" &&
                  f.FechaEmision >= inicioMesUtc && f.FechaEmision < inicioMesSiguienteUtc)));

        var facturasVencidas = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxC" &&
                        m.MontoPendiente > 0 &&
                        m.FechaVencimiento < hoyDate &&
                        m.Factura.Estado != "PAGADA" &&
                        m.Factura.Estado != "ANULADA")
            .Select(m => m.IdFactura)
            .Distinct()
            .CountAsync();

        var totalPropiedades = await _context.Propiedades.CountAsync(p => p.Activo);
        var propiedadesOcupadas = await _context.Propiedades.CountAsync(p => p.Activo && p.Estado == "Alquilada");
        var propiedadesDisponibles = await _context.Propiedades.CountAsync(p => p.Activo && p.Estado == "Disponible");
        var contratosActivos = await _context.Contratos.CountAsync(c => c.Estado == "Activo");
        var contratosPorVencer = await _context.Contratos.CountAsync(c =>
            c.Estado == "Activo" &&
            c.FechaVencimiento >= hoyDate &&
            c.FechaVencimiento <= hoyDate.AddDays(30));
        var tasaOcupacion = totalPropiedades > 0
            ? Math.Round((decimal)propiedadesOcupadas / totalPropiedades * 100, 1)
            : 0m;

        var meses = Enumerable.Range(0, 6)
            .Select(indice => inicioMesLocal.AddMonths(indice - 5))
            .ToList();
        var periodos = meses.Select(m => $"{m.Year:D4}-{m.Month:D2}").ToList();
        var primerMesUtc = meses[0].ToUniversalTime();

        var facturasTendencia = await _context.FacturasCabecera
            .Where(f => f.Estado != "ANULADA" &&
                ((f.PeriodoFacturado != null && periodos.Contains(f.PeriodoFacturado)) ||
                 (f.PeriodoFacturado == null &&
                  f.FechaEmision >= primerMesUtc && f.FechaEmision < inicioMesSiguienteUtc)))
            .Select(f => new
            {
                f.PeriodoFacturado,
                f.FechaEmision,
                f.Total
            })
            .ToListAsync();

        var pagosTendencia = await _context.Pagos
            .Where(p => p.FechaPago >= primerMesUtc && p.FechaPago < inicioMesSiguienteUtc)
            .Select(p => new { p.FechaPago, p.Monto })
            .ToListAsync();

        var tendencia = periodos
            .Select(periodo => new SerieMensualDashboardResponse
            {
                Periodo = periodo,
                Facturado = facturasTendencia
                    .Where(f => ObtenerPeriodoFactura(f.PeriodoFacturado, f.FechaEmision) == periodo)
                    .Sum(f => f.Total),
                Cobrado = pagosTendencia
                    .Where(p => p.FechaPago.ToLocalTime().ToString("yyyy-MM") == periodo)
                    .Sum(p => p.Monto)
            })
            .ToList();

        var ultimasEntidades = await _context.FacturasCabecera
            .Where(f => f.Estado != "ANULADA")
            .Include(f => f.Entidad)
            .Include(f => f.Pagos)
            .OrderByDescending(f => f.FechaEmision)
            .Take(6)
            .ToListAsync();

        var ultimasFacturas = ultimasEntidades.Select(f =>
        {
            var pagado = f.Pagos.Sum(p => p.Monto);
            return new UltimaFacturaDashboardResponse
            {
                IdFactura = f.IdFactura,
                NumeroECF = f.NumeroECF,
                PeriodoFacturado = f.PeriodoFacturado,
                RazonSocial = f.Entidad.RazonSocial,
                Total = f.Total,
                MontoPagado = pagado,
                MontoPendiente = Math.Max(0m, f.Total - pagado),
                Estado = f.Estado,
                TipoFactura = f.TipoFactura,
                FechaEmision = f.FechaEmision,
                EsPeriodoFuturo = f.Estado != "ANULADA" &&
                                  !string.IsNullOrWhiteSpace(f.PeriodoFacturado) &&
                                  string.CompareOrdinal(f.PeriodoFacturado, periodoActual) > 0
            };
        }).ToList();

        return Ok(new MetricasDashboardResponse
        {
            PeriodoActual = periodoActual,
            FacturadoMes = facturadoMes,
            CobradoMes = cobradoMes,
            GastosMes = gastosMes,
            TotalCxC = totalCxC,
            TotalCxP = totalCxP,
            MargenGanancia = cobradoMes - gastosMes,
            FacturasEmitidas = facturasEmitidas,
            FacturasVencidas = facturasVencidas,
            TotalPropiedades = totalPropiedades,
            PropiedadesOcupadas = propiedadesOcupadas,
            PropiedadesDisponibles = propiedadesDisponibles,
            ContratosActivos = contratosActivos,
            ContratosPorVencer = contratosPorVencer,
            TasaOcupacion = tasaOcupacion,
            TendenciaMensual = tendencia,
            UltimasFacturas = ultimasFacturas
        });
    }

    private static string ObtenerPeriodoFactura(string? periodoFacturado, DateTime fechaEmision)
    {
        return !string.IsNullOrWhiteSpace(periodoFacturado)
            ? periodoFacturado
            : fechaEmision.ToLocalTime().ToString("yyyy-MM");
    }
}
