using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Propiedades;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropiedadesController : ControllerBase
{
    private static readonly string[] EstadosValidos = ["Disponible", "Alquilada", "Mantenimiento", "Inactiva"];
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public PropiedadesController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PropiedadesVer)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] int? idPropietario,
        [FromQuery] bool incluirInactivas = false)
    {
        var query = _context.Propiedades
            .Include(p => p.Entidad)
            .Include(p => p.Unidades)
            .AsQueryable();

        if (!incluirInactivas)
            query = query.Where(p => p.Activo);
        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(p => p.TipoPropiedad == tipo);
        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => p.Estado == estado);
        if (idPropietario.HasValue)
            query = query.Where(p => p.IdEntidad == idPropietario.Value);

        var propiedades = await query
            .OrderBy(p => p.Codigo)
            .ToListAsync();

        return Ok(propiedades.Select(MapResponse));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.PropiedadesVer)]
    public async Task<IActionResult> GetById(int id)
    {
        var propiedad = await _context.Propiedades
            .Include(p => p.Entidad)
            .Include(p => p.Unidades)
            .FirstOrDefaultAsync(p => p.IdPropiedad == id);

        return propiedad is null
            ? NotFound(new { mensaje = "Propiedad no encontrada." })
            : Ok(MapResponse(propiedad));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> Create([FromBody] CreatePropiedadRequest request)
    {
        var propietario = await _context.Entidades.FindAsync(request.IdEntidad);
        if (propietario is null || !propietario.Activo)
            return BadRequest(new { mensaje = "El propietario no existe o está inactivo." });

        if (propietario.Tipo != "Propietario")
            return BadRequest(new { mensaje = "La entidad seleccionada debe ser de tipo Propietario." });

        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await _context.Propiedades.AnyAsync(p => p.Codigo == codigo))
            return Conflict(new { mensaje = "Ya existe una propiedad con ese código." });

        var propiedad = new Propiedad
        {
            IdEntidad = request.IdEntidad,
            Codigo = codigo,
            TipoPropiedad = request.TipoPropiedad.Trim(),
            Direccion = request.Direccion.Trim(),
            Sector = request.Sector?.Trim(),
            Ciudad = request.Ciudad?.Trim(),
            MetrosCuadrados = request.MetrosCuadrados,
            CantidadHabitaciones = request.CantidadHabitaciones,
            CantidadBanos = request.CantidadBanos,
            TieneParqueo = request.TieneParqueo,
            CanonMensualSugerido = request.CanonMensualSugerido,
            MantenimientoMensualSugerido = request.MantenimientoMensualSugerido,
            Estado = "Disponible",
            Activo = true
        };

        _context.Propiedades.Add(propiedad);
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Propiedades", propiedad.IdPropiedad, propiedad.Codigo);

        propiedad.Entidad = propietario;
        return CreatedAtAction(nameof(GetById), new { id = propiedad.IdPropiedad }, MapResponse(propiedad));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePropiedadRequest request)
    {
        var propiedad = await _context.Propiedades
            .Include(p => p.Entidad)
            .Include(p => p.Unidades)
            .FirstOrDefaultAsync(p => p.IdPropiedad == id);
        if (propiedad is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        var propietario = await _context.Entidades.FindAsync(request.IdEntidad);
        if (propietario is null || !propietario.Activo)
            return BadRequest(new { mensaje = "El propietario no existe o está inactivo." });
        if (propietario.Tipo != "Propietario")
            return BadRequest(new { mensaje = "La entidad seleccionada debe ser de tipo Propietario." });

        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await _context.Propiedades.AnyAsync(p => p.Codigo == codigo && p.IdPropiedad != id))
            return Conflict(new { mensaje = "Ya existe otra propiedad con ese código." });

        propiedad.IdEntidad = request.IdEntidad;
        propiedad.Codigo = codigo;
        propiedad.TipoPropiedad = request.TipoPropiedad.Trim();
        propiedad.Direccion = request.Direccion.Trim();
        propiedad.Sector = request.Sector?.Trim();
        propiedad.Ciudad = request.Ciudad?.Trim();
        propiedad.MetrosCuadrados = request.MetrosCuadrados;
        propiedad.CantidadHabitaciones = request.CantidadHabitaciones;
        propiedad.CantidadBanos = request.CantidadBanos;
        propiedad.TieneParqueo = request.TieneParqueo;
        propiedad.CanonMensualSugerido = request.CanonMensualSugerido;
        propiedad.MantenimientoMensualSugerido = request.MantenimientoMensualSugerido;

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Propiedades", propiedad.IdPropiedad);

        propiedad.Entidad = propietario;
        return Ok(MapResponse(propiedad));
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] string nuevoEstado)
    {
        var estado = EstadosValidos.FirstOrDefault(e => e.Equals(nuevoEstado?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (estado is null)
            return BadRequest(new { mensaje = $"Estado inválido. Valores permitidos: {string.Join(", ", EstadosValidos)}." });

        var propiedad = await _context.Propiedades.FindAsync(id);
        if (propiedad is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        propiedad.Estado = estado;
        propiedad.Activo = estado != "Inactiva";
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Propiedades", propiedad.IdPropiedad, $"Estado: {estado}");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.PropiedadesGestionar)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var propiedad = await _context.Propiedades
            .Include(p => p.Contratos)
            .FirstOrDefaultAsync(p => p.IdPropiedad == id);
        if (propiedad is null)
            return NotFound(new { mensaje = "Propiedad no encontrada." });

        if (propiedad.Contratos.Any(c => c.Estado is "Activo" or "Pendiente"))
            return BadRequest(new { mensaje = "No se puede desactivar una propiedad con contratos activos o pendientes." });

        propiedad.Activo = false;
        propiedad.Estado = "Inactiva";
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "ELIMINAR", "Propiedades", propiedad.IdPropiedad);
        return NoContent();
    }

    private static PropiedadResponse MapResponse(Propiedad p) => new()
    {
        IdPropiedad = p.IdPropiedad,
        IdEntidad = p.IdEntidad,
        RazonSocialPropietario = p.Entidad.RazonSocial,
        RncCedulaPropietario = p.Entidad.RncCedula,
        Codigo = p.Codigo,
        TipoPropiedad = p.TipoPropiedad,
        Direccion = p.Direccion,
        Sector = p.Sector,
        Ciudad = p.Ciudad,
        MetrosCuadrados = p.MetrosCuadrados,
        CantidadHabitaciones = p.CantidadHabitaciones,
        CantidadBanos = p.CantidadBanos,
        TieneParqueo = p.TieneParqueo,
        CanonMensualSugerido = p.CanonMensualSugerido,
        MantenimientoMensualSugerido = p.MantenimientoMensualSugerido,
        Estado = p.Estado,
        Activo = p.Activo,
        CantidadUnidades = p.Unidades.Count,
        CantidadUnidadesOcupadas = p.Unidades.Count(u => u.Estado == "Alquilada")
    };
}
