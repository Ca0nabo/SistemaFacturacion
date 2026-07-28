using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Propiedades;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropiedadesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public PropiedadesController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? tipo, [FromQuery] string? estado)
    {
        var query = _context.Propiedades
            .Include(p => p.Entidad)
            .Include(p => p.Unidades)
            .AsQueryable();

        if (!string.IsNullOrEmpty(tipo))
            query = query.Where(p => p.TipoPropiedad == tipo);
        if (!string.IsNullOrEmpty(estado))
            query = query.Where(p => p.Estado == estado);

        var propiedades = await query
            .OrderByDescending(p => p.IdPropiedad)
            .Select(p => new PropiedadResponse
            {
                IdPropiedad = p.IdPropiedad,
                IdEntidad = p.IdEntidad,
                RazonSocialPropietario = p.Entidad.RazonSocial,
                RncCedulaPropietario = p.Entidad.RncCedula,
                TipoPropiedad = p.TipoPropiedad,
                Direccion = p.Direccion,
                Sector = p.Sector,
                Ciudad = p.Ciudad,
                MetrosCuadrados = p.MetrosCuadrados,
                CantidadHabitaciones = p.CantidadHabitaciones,
                CantidadBanos = p.CantidadBanos,
                TieneParqueo = p.TieneParqueo,
                Estado = p.Estado,
                Activo = p.Activo,
                CantidadUnidades = p.Unidades.Count,
                CantidadUnidadesOcupadas = p.Unidades.Count(u => u.Estado == "Alquilada")
            })
            .ToListAsync();

        return Ok(propiedades);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _context.Propiedades
            .Include(p => p.Entidad)
            .Include(p => p.Unidades)
            .FirstOrDefaultAsync(p => p.IdPropiedad == id);

        if (p is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        return Ok(new PropiedadResponse
        {
            IdPropiedad = p.IdPropiedad,
            IdEntidad = p.IdEntidad,
            RazonSocialPropietario = p.Entidad.RazonSocial,
            RncCedulaPropietario = p.Entidad.RncCedula,
            TipoPropiedad = p.TipoPropiedad,
            Direccion = p.Direccion,
            Sector = p.Sector,
            Ciudad = p.Ciudad,
            MetrosCuadrados = p.MetrosCuadrados,
            CantidadHabitaciones = p.CantidadHabitaciones,
            CantidadBanos = p.CantidadBanos,
            TieneParqueo = p.TieneParqueo,
            Estado = p.Estado,
            Activo = p.Activo,
            CantidadUnidades = p.Unidades.Count,
            CantidadUnidadesOcupadas = p.Unidades.Count(u => u.Estado == "Alquilada")
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropiedadRequest request)
    {
        var entidad = await _context.Entidades.FindAsync(request.IdEntidad);
        if (entidad is null)
            return BadRequest(new { mensaje = "El propietario no existe." });

        var propiedad = new Propiedad
        {
            IdEntidad = request.IdEntidad,
            TipoPropiedad = request.TipoPropiedad,
            Direccion = request.Direccion,
            Sector = request.Sector,
            Ciudad = request.Ciudad,
            MetrosCuadrados = request.MetrosCuadrados,
            CantidadHabitaciones = request.CantidadHabitaciones,
            CantidadBanos = request.CantidadBanos,
            TieneParqueo = request.TieneParqueo,
            Estado = "Disponible",
            Activo = true
        };

        _context.Propiedades.Add(propiedad);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Propiedades", propiedad.IdPropiedad);

        return CreatedAtAction(nameof(GetById), new { id = propiedad.IdPropiedad }, new PropiedadResponse
        {
            IdPropiedad = propiedad.IdPropiedad,
            IdEntidad = propiedad.IdEntidad,
            RazonSocialPropietario = entidad.RazonSocial,
            RncCedulaPropietario = entidad.RncCedula,
            TipoPropiedad = propiedad.TipoPropiedad,
            Direccion = propiedad.Direccion,
            Sector = propiedad.Sector,
            Ciudad = propiedad.Ciudad,
            MetrosCuadrados = propiedad.MetrosCuadrados,
            CantidadHabitaciones = propiedad.CantidadHabitaciones,
            CantidadBanos = propiedad.CantidadBanos,
            TieneParqueo = propiedad.TieneParqueo,
            Estado = propiedad.Estado,
            Activo = propiedad.Activo
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePropiedadRequest request)
    {
        var propiedad = await _context.Propiedades.FindAsync(id);
        if (propiedad is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        propiedad.IdEntidad = request.IdEntidad;
        propiedad.TipoPropiedad = request.TipoPropiedad;
        propiedad.Direccion = request.Direccion;
        propiedad.Sector = request.Sector;
        propiedad.Ciudad = request.Ciudad;
        propiedad.MetrosCuadrados = request.MetrosCuadrados;
        propiedad.CantidadHabitaciones = request.CantidadHabitaciones;
        propiedad.CantidadBanos = request.CantidadBanos;
        propiedad.TieneParqueo = request.TieneParqueo;

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Propiedades", propiedad.IdPropiedad);

        return NoContent();
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        var propiedad = await _context.Propiedades.FindAsync(id);
        if (propiedad is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        propiedad.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var propiedad = await _context.Propiedades.Include(p => p.Contratos).FirstOrDefaultAsync(p => p.IdPropiedad == id);
        if (propiedad is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        if (propiedad.Contratos.Any(c => c.Estado == "Activo" || c.Estado == "Pendiente"))
            return BadRequest(new { mensaje = "No se puede desactivar una propiedad con contratos activos." });

        propiedad.Activo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
