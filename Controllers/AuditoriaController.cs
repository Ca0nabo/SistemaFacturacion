using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Auditoria;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditoriaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.AuditoriaVer)]
    public async Task<IActionResult> GetAll([FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
    {
        var query = _context.AuditoriaLogs
            .Include(l => l.Usuario)
            .OrderByDescending(l => l.FechaRegistro);

        var total = await query.CountAsync();

        var logs = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(l => new AuditoriaLogResponse
            {
                IdLog = l.IdLog,
                IdUsuario = l.IdUsuario,
                EmailUsuario = l.Usuario.Email,
                NombreUsuario = l.Usuario.NombreCompleto,
                Accion = l.Accion,
                Modulo = l.Modulo,
                IdRegistro = l.IdRegistro,
                Detalle = l.Detalle,
                FechaRegistro = l.FechaRegistro
            })
            .ToListAsync();

        return Ok(new { total, pagina, tamano, items = logs });
    }

    [HttpGet("modulo/{modulo}")]
    [Authorize(Policy = Permissions.AuditoriaVer)]
    public async Task<IActionResult> GetByModulo(string modulo, [FromQuery] int pagina = 1, [FromQuery] int tamano = 50)
    {
        var query = _context.AuditoriaLogs
            .Include(l => l.Usuario)
            .Where(l => l.Modulo == modulo)
            .OrderByDescending(l => l.FechaRegistro);

        var total = await query.CountAsync();

        var logs = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(l => new AuditoriaLogResponse
            {
                IdLog = l.IdLog,
                IdUsuario = l.IdUsuario,
                EmailUsuario = l.Usuario.Email,
                NombreUsuario = l.Usuario.NombreCompleto,
                Accion = l.Accion,
                Modulo = l.Modulo,
                IdRegistro = l.IdRegistro,
                Detalle = l.Detalle,
                FechaRegistro = l.FechaRegistro
            })
            .ToListAsync();

        return Ok(new { total, pagina, tamano, modulo, items = logs });
    }
}
