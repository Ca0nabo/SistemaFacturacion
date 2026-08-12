using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Alertas;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AlertasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.AlertasVer)]
    public async Task<IActionResult> GetAlertas([FromQuery] int dias = 7)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var fechaLimite = hoy.AddDays(dias);
        var alertas = new List<AlertaResponse>();

        var facturasVencidas = await _context.MovimientosCx
            .Include(m => m.Factura)
            .ThenInclude(f => f.Entidad)
            .Where(m => m.MontoPendiente > 0 && m.FechaVencimiento <= fechaLimite)
            .ToListAsync();

        foreach (var m in facturasVencidas)
        {
            alertas.Add(new AlertaResponse
            {
                Id = m.IdMovimiento,
                Tipo = "Factura",
                Referencia = m.Factura.NumeroECF,
                Entidad = m.Factura.Entidad.RazonSocial,
                RncCedula = m.Factura.Entidad.RncCedula,
                Monto = m.MontoPendiente,
                Estado = m.Factura.Estado,
                FechaVencimiento = m.FechaVencimiento,
                Criticidad = m.FechaVencimiento < hoy ? "Vencido" : "Próximo a vencer"
            });
        }

        var contratos = await _context.Contratos
            .Include(c => c.Entidad)
            .Where(c => c.Estado != "Cancelado" && c.FechaVencimiento <= fechaLimite)
            .ToListAsync();

        foreach (var c in contratos)
        {
            alertas.Add(new AlertaResponse
            {
                Id = c.IdContrato,
                Tipo = "Contrato",
                Referencia = $"Contrato #{c.IdContrato}",
                Entidad = c.Entidad.RazonSocial,
                RncCedula = c.Entidad.RncCedula,
                Monto = c.Monto,
                Estado = c.Estado,
                FechaVencimiento = c.FechaVencimiento,
                Criticidad = c.FechaVencimiento < hoy ? "Vencido" : "Próximo a vencer"
            });
        }

        return Ok(alertas.OrderBy(a => a.Criticidad).ThenBy(a => a.FechaVencimiento));
    }

    [HttpGet("contador")]
    [Authorize(Policy = Permissions.AlertasVer)]
    public async Task<IActionResult> GetContador()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var vencidas = await _context.MovimientosCx
            .CountAsync(m => m.MontoPendiente > 0 && m.FechaVencimiento < hoy);

        var proximas = await _context.MovimientosCx
            .CountAsync(m => m.MontoPendiente > 0 && m.FechaVencimiento >= hoy && m.FechaVencimiento <= hoy.AddDays(7));

        var contratosVencidos = await _context.Contratos
            .CountAsync(c => c.Estado != "Cancelado" && c.FechaVencimiento < hoy);

        var contratosProximos = await _context.Contratos
            .CountAsync(c => c.Estado != "Cancelado" && c.FechaVencimiento >= hoy && c.FechaVencimiento <= hoy.AddDays(7));

        return Ok(new
        {
            total = vencidas + proximas + contratosVencidos + contratosProximos,
            vencidas,
            proximasAVencer = proximas,
            contratosVencidos,
            contratosProximosAVencer = contratosProximos
        });
    }
}
