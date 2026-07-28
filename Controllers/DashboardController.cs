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

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var hoyDate = DateOnly.FromDateTime(hoy);

        var ingresosMes = await _context.FacturasCabecera
            .Where(f => f.Estado == "EMITIDA" && f.FechaEmision >= inicioMes)
            .SumAsync(f => f.Total);

        var gastosMes = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxP" && m.MontoPendiente == 0)
            .SumAsync(m => m.MontoOriginal);

        var totalCxC = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxC" && m.MontoPendiente > 0)
            .SumAsync(m => m.MontoPendiente);

        var totalCxP = await _context.MovimientosCx
            .Where(m => m.Tipo == "CxP" && m.MontoPendiente > 0)
            .SumAsync(m => m.MontoPendiente);

        var facturasEmitidas = await _context.FacturasCabecera
            .CountAsync(f => f.FechaEmision >= inicioMes);

        var movimientosVencidos = await _context.MovimientosCx
            .CountAsync(m => m.MontoPendiente > 0 && m.FechaVencimiento < hoyDate);

        var totalPropiedades = await _context.Propiedades.CountAsync(p => p.Activo);
        var propiedadesOcupadas = await _context.Propiedades.CountAsync(p => p.Activo && p.Estado == "Alquilada");
        var propiedadesDisponibles = await _context.Propiedades.CountAsync(p => p.Activo && p.Estado == "Disponible");
        var contratosActivos = await _context.Contratos.CountAsync(c => c.Estado == "Activo");
        var contratosPorVencer = await _context.Contratos.CountAsync(c =>
            c.Estado == "Activo" && c.FechaVencimiento >= hoyDate && c.FechaVencimiento <= hoyDate.AddDays(30));
        var tasaOcupacion = totalPropiedades > 0
            ? Math.Round((decimal)propiedadesOcupadas / totalPropiedades * 100, 1)
            : 0;

        return Ok(new MetricasDashboardResponse
        {
            IngresosMes = ingresosMes,
            GastosMes = gastosMes,
            TotalCxC = totalCxC,
            TotalCxP = totalCxP,
            MargenGanancia = ingresosMes - gastosMes,
            FacturasEmitidas = facturasEmitidas,
            MovimientosVencidos = movimientosVencidos,
            TotalPropiedades = totalPropiedades,
            PropiedadesOcupadas = propiedadesOcupadas,
            PropiedadesDisponibles = propiedadesDisponibles,
            ContratosActivos = contratosActivos,
            ContratosPorVencer = contratosPorVencer,
            TasaOcupacion = tasaOcupacion
        });
    }
}
