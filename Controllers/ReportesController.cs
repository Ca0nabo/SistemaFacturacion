using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Reportes;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("ingresos-mensuales")]
    public async Task<IActionResult> GetIngresosMensuales([FromQuery] int? anio)
    {
        anio ??= DateTime.UtcNow.Year;

        var facturas = await _context.FacturasCabecera
            .Where(f => f.FechaEmision.Year == anio && f.Estado == "EMITIDA")
            .ToListAsync();

        var reporte = facturas
            .GroupBy(f => f.FechaEmision.Month)
            .Select(g => new ReporteIngresosResponse
            {
                Anio = anio.Value,
                Mes = g.Key,
                MesNombre = new DateTime(anio.Value, g.Key, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-DO")),
                TotalIngresos = g.Sum(f => f.Total),
                CantidadFacturas = g.Count()
            })
            .OrderBy(r => r.Mes)
            .ToList();

        return Ok(reporte);
    }

    [HttpGet("facturacion-por-cliente")]
    public async Task<IActionResult> GetFacturacionPorCliente([FromQuery] int? anio)
    {
        anio ??= DateTime.UtcNow.Year;

        var query = _context.FacturasCabecera
            .Include(f => f.Entidad)
            .Where(f => f.FechaEmision.Year == anio && f.Estado == "EMITIDA");

        var reporte = await query
            .GroupBy(f => new { f.IdEntidad, f.Entidad.RazonSocial, f.Entidad.RncCedula })
            .Select(g => new ReporteFacturacionClienteResponse
            {
                IdEntidad = g.Key.IdEntidad,
                RazonSocial = g.Key.RazonSocial,
                RncCedula = g.Key.RncCedula,
                TotalFacturado = g.Sum(f => f.Total),
                CantidadFacturas = g.Count()
            })
            .OrderByDescending(r => r.TotalFacturado)
            .ToListAsync();

        return Ok(reporte);
    }

    [HttpGet("estado-cuentas")]
    public async Task<IActionResult> GetEstadoCuentas()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var totalPendiente = await _context.MovimientosCx
            .Where(m => m.MontoPendiente > 0 && m.FechaVencimiento >= hoy)
            .SumAsync(m => m.MontoPendiente);

        var totalVencido = await _context.MovimientosCx
            .Where(m => m.MontoPendiente > 0 && m.FechaVencimiento < hoy)
            .SumAsync(m => m.MontoPendiente);

        var totalPagado = await _context.MovimientosCx
            .Where(m => m.MontoPendiente == 0)
            .SumAsync(m => m.MontoOriginal);

        var cantidadPendiente = await _context.MovimientosCx
            .CountAsync(m => m.MontoPendiente > 0 && m.FechaVencimiento >= hoy);

        var cantidadVencido = await _context.MovimientosCx
            .CountAsync(m => m.MontoPendiente > 0 && m.FechaVencimiento < hoy);

        return Ok(new ReporteCxCResponse
        {
            TotalPendiente = totalPendiente,
            TotalVencido = totalVencido,
            TotalPagado = totalPagado,
            CantidadPendiente = cantidadPendiente,
            CantidadVencido = cantidadVencido
        });
    }

    [HttpGet("ingresos-por-propiedad")]
    public async Task<IActionResult> GetIngresosPorPropiedad([FromQuery] int? anio)
    {
        anio ??= DateTime.UtcNow.Year;

        var query = _context.FacturasCabecera
            .Include(f => f.Propiedad)
            .Where(f => f.FechaEmision.Year == anio && f.Estado == "EMITIDA" && f.IdPropiedad != null);

        var reporte = await query
            .GroupBy(f => new { f.IdPropiedad, Direccion = f.Propiedad!.Direccion })
            .Select(g => new
            {
                IdPropiedad = g.Key.IdPropiedad,
                Direccion = g.Key.Direccion,
                TotalIngresos = g.Sum(f => f.Total),
                CantidadFacturas = g.Count()
            })
            .OrderByDescending(r => r.TotalIngresos)
            .ToListAsync();

        return Ok(reporte);
    }

    [HttpGet("morosidad")]
    public async Task<IActionResult> GetMorosidad()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var morosidad = await _context.MovimientosCx
            .Include(m => m.Factura)
            .ThenInclude(f => f.Entidad)
            .Include(m => m.Propiedad)
            .Where(m => m.Tipo == "CxC" && m.MontoPendiente > 0)
            .OrderByDescending(m => m.MontoPendiente)
            .Select(m => new
            {
                IdMovimiento = m.IdMovimiento,
                IdFactura = m.IdFactura,
                NumeroECF = m.Factura.NumeroECF,
                Entidad = m.Factura.Entidad.RazonSocial,
                IdPropiedad = m.IdPropiedad,
                DireccionPropiedad = m.Propiedad != null ? m.Propiedad.Direccion : null,
                MontoPendiente = m.MontoPendiente,
                FechaVencimiento = m.FechaVencimiento,
                DiasVencido = hoy.DayNumber - m.FechaVencimiento.DayNumber
            })
            .ToListAsync();

        return Ok(morosidad);
    }

    [HttpGet("ocupacion")]
    public async Task<IActionResult> GetOcupacion()
    {
        var total = await _context.Propiedades.CountAsync(p => p.Activo);
        var ocupadas = await _context.Propiedades.CountAsync(p => p.Activo && p.Estado == "Alquilada");
        var disponibles = await _context.Propiedades.CountAsync(p => p.Activo && p.Estado == "Disponible");

        return Ok(new
        {
            Total = total,
            Ocupadas = ocupadas,
            Disponibles = disponibles,
            TasaOcupacion = total > 0 ? Math.Round((decimal)ocupadas / total * 100, 1) : 0
        });
    }
}
