using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Roles;
using SistemaFacturacion.Models;
using SistemaFacturacion.Security;
using SistemaFacturacion.Services;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;

    public RolesController(ApplicationDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesVer)]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(r => r.IdRol)
            .Select(r => new
            {
                r.IdRol,
                r.Nombre,
                r.Permisos,
                Usuarios = r.Usuarios.Count
            })
            .ToListAsync();

        return Ok(roles.Select(r => new
        {
            r.IdRol,
            r.Nombre,
            permisos = Permissions.Expand(r.Permisos),
            r.Usuarios,
            esSistema = r.IdRol <= 6,
            protegido = r.IdRol == 1
        }));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.RolesVer)]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.IdRol == id);
        if (role is null) return NotFound(new { mensaje = "Rol no encontrado." });
        return Ok(new
        {
            role.IdRol,
            role.Nombre,
            permisos = Permissions.Expand(role.Permisos),
            esSistema = role.IdRol <= 6,
            protegido = role.IdRol == 1
        });
    }

    [HttpGet("permisos")]
    [Authorize(Policy = Permissions.RolesVer)]
    public IActionResult GetPermissionCatalog()
    {
        var groups = Permissions.Catalog
            .GroupBy(p => p.Module)
            .Select(g => new
            {
                modulo = g.Key,
                permisos = g.Select(p => new
                {
                    key = p.Key,
                    accion = p.Action,
                    descripcion = p.Description
                })
            });
        return Ok(groups);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesGestionar)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var name = request.Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { mensaje = "El nombre del rol es obligatorio." });
        if (name.Length > 50) return BadRequest(new { mensaje = "El nombre del rol no puede superar 50 caracteres." });
        if (await _db.Roles.AnyAsync(r => r.Nombre.ToLower() == name.ToLower()))
            return Conflict(new { mensaje = "Ya existe un rol con ese nombre." });

        var permisos = NormalizeAndValidatePermissions(request.Permisos, out var error);
        if (error is not null) return BadRequest(new { mensaje = error });

        var role = new Role { Nombre = name, Permisos = Permissions.Serialize(permisos) };
        _db.Roles.Add(role);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "No se pudo guardar el rol en la base de datos. Reinicia HabitaCont una vez para que el sistema sincronice las secuencias de PostgreSQL y vuelve a intentarlo."
            });
        }

        await _audit.LogFromContextAsync(HttpContext, "CREAR", "Roles", role.IdRol, $"Rol {role.Nombre} creado con {permisos.Count} permisos");
        return CreatedAtAction(nameof(GetById), new { id = role.IdRol }, new { role.IdRol, role.Nombre, permisos });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.RolesGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.IdRol == id);
        if (role is null) return NotFound(new { mensaje = "Rol no encontrado." });
        if (id == 1) return BadRequest(new { mensaje = "El rol Administrador está protegido y conserva acceso total." });

        var name = request.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { mensaje = "El nombre del rol es obligatorio." });
        if (await _db.Roles.AnyAsync(r => r.IdRol != id && r.Nombre.ToLower() == name.ToLower()))
            return Conflict(new { mensaje = "Ya existe otro rol con ese nombre." });

        var permisos = NormalizeAndValidatePermissions(request.Permisos, out var error);
        if (error is not null) return BadRequest(new { mensaje = error });

        role.Nombre = name;
        role.Permisos = Permissions.Serialize(permisos);
        await _db.SaveChangesAsync();
        await _audit.LogFromContextAsync(HttpContext, "EDITAR", "Roles", role.IdRol, $"Rol {role.Nombre} actualizado con {permisos.Count} permisos");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.RolesGestionar)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 6)
            return BadRequest(new { mensaje = "Los roles base del sistema no se eliminan. Puedes modificar sus permisos, excepto Administrador." });

        var role = await _db.Roles.Include(r => r.Usuarios).FirstOrDefaultAsync(r => r.IdRol == id);
        if (role is null) return NotFound(new { mensaje = "Rol no encontrado." });
        if (role.Usuarios.Count > 0)
            return BadRequest(new { mensaje = "No se puede eliminar un rol que tiene usuarios asignados." });

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        await _audit.LogFromContextAsync(HttpContext, "ELIMINAR", "Roles", id, $"Rol {role.Nombre} eliminado");
        return NoContent();
    }

    private static List<string> NormalizeAndValidatePermissions(IEnumerable<string>? permissions, out string? error)
    {
        error = null;
        var list = (permissions ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invalid = list.Where(p => p != Permissions.Todo && !Permissions.Keys.Contains(p)).ToList();
        if (invalid.Count > 0)
        {
            error = $"Permisos no reconocidos: {string.Join(", ", invalid)}";
            return [];
        }

        if (list.Contains(Permissions.Todo, StringComparer.OrdinalIgnoreCase))
            return Permissions.Catalog.Select(p => p.Key).ToList();

        return list;
    }
}
