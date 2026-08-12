using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Unidades;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnidadesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public UnidadesController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PropiedadesVer)]
    public async Task<IActionResult> GetAll([FromQuery] int? idPropiedad, [FromQuery] string? estado)
    {
        var query = _context.Unidades
            .Include(u => u.Propiedad)
            .AsQueryable();

        if (idPropiedad.HasValue)
            query = query.Where(u => u.IdPropiedad == idPropiedad.Value);
        if (!string.IsNullOrEmpty(estado))
            query = query.Where(u => u.Estado == estado);

        var unidades = await query
            .OrderBy(u => u.Codigo)
            .Select(u => new UnidadResponse
            {
                IdUnidad = u.IdUnidad,
                IdPropiedad = u.IdPropiedad,
                DireccionPropiedad = u.Propiedad.Direccion,
                Codigo = u.Codigo,
                Piso = u.Piso,
                MetrosCuadrados = u.MetrosCuadrados,
                Estado = u.Estado,
                Activo = u.Activo
            })
            .ToListAsync();

        return Ok(unidades);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.PropiedadesVer)]
    public async Task<IActionResult> GetById(int id)
    {
        var unidad = await _context.Unidades
            .Include(u => u.Propiedad)
            .FirstOrDefaultAsync(u => u.IdUnidad == id);

        if (unidad is null)
            return NotFound(new { mensaje = "Unidad no encontrada." });

        return Ok(new UnidadResponse
        {
            IdUnidad = unidad.IdUnidad,
            IdPropiedad = unidad.IdPropiedad,
            DireccionPropiedad = unidad.Propiedad.Direccion,
            Codigo = unidad.Codigo,
            Piso = unidad.Piso,
            MetrosCuadrados = unidad.MetrosCuadrados,
            Estado = unidad.Estado,
            Activo = unidad.Activo
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> Create([FromBody] CreateUnidadRequest request)
    {
        var propiedad = await _context.Propiedades.FindAsync(request.IdPropiedad);
        if (propiedad is null)
            return BadRequest(new { mensaje = "La propiedad no existe." });

        if (await _context.Unidades.AnyAsync(u => u.IdPropiedad == request.IdPropiedad && u.Codigo == request.Codigo))
            return Conflict(new { mensaje = "Ya existe una unidad con ese código en esta propiedad." });

        var unidad = new Unidad
        {
            IdPropiedad = request.IdPropiedad,
            Codigo = request.Codigo,
            Piso = request.Piso,
            MetrosCuadrados = request.MetrosCuadrados,
            Estado = "Disponible",
            Activo = true
        };

        _context.Unidades.Add(unidad);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Unidades", unidad.IdUnidad);

        return CreatedAtAction(nameof(GetById), new { id = unidad.IdUnidad }, new UnidadResponse
        {
            IdUnidad = unidad.IdUnidad,
            IdPropiedad = unidad.IdPropiedad,
            DireccionPropiedad = propiedad.Direccion,
            Codigo = unidad.Codigo,
            Piso = unidad.Piso,
            MetrosCuadrados = unidad.MetrosCuadrados,
            Estado = unidad.Estado,
            Activo = unidad.Activo
        });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUnidadRequest request)
    {
        var unidad = await _context.Unidades.FindAsync(id);
        if (unidad is null)
            return NotFound(new { mensaje = "Unidad no encontrada." });

        if (await _context.Unidades.AnyAsync(u => u.IdPropiedad == request.IdPropiedad && u.Codigo == request.Codigo && u.IdUnidad != id))
            return Conflict(new { mensaje = "Ya existe otra unidad con ese código en esta propiedad." });

        unidad.IdPropiedad = request.IdPropiedad;
        unidad.Codigo = request.Codigo;
        unidad.Piso = request.Piso;
        unidad.MetrosCuadrados = request.MetrosCuadrados;

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Unidades", unidad.IdUnidad);

        return NoContent();
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        var unidad = await _context.Unidades.FindAsync(id);
        if (unidad is null)
            return NotFound(new { mensaje = "Unidad no encontrada." });

        unidad.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var unidad = await _context.Unidades.FindAsync(id);
        if (unidad is null)
            return NotFound(new { mensaje = "Unidad no encontrada." });

        unidad.Activo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
