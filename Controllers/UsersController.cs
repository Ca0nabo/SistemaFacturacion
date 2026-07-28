using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Auth;
using SistemaFacturacion.Services;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public UsersController(IAuthService authService, ApplicationDbContext context, IAuditLogService auditLog)
    {
        _authService = authService;
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _authService.GetAllUsersAsync();
        var result = users.Select(u => new
        {
            u.IdUsuario,
            u.Email,
            u.NombreCompleto,
            Rol = u.Rol.Nombre,
            u.IdRol,
            u.Activo,
            u.FechaCreacion
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _authService.GetUserByIdAsync(id);
        if (user is null)
            return NotFound(new { mensaje = "Usuario no encontrado." });

        return Ok(new
        {
            user.IdUsuario,
            user.Email,
            user.NombreCompleto,
            Rol = user.Rol.Nombre,
            user.IdRol,
            user.Activo,
            user.FechaCreacion
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Usuarios", response.IdUsuario, $"Usuario {request.Email} creado");
            return CreatedAtAction(nameof(GetById), new { id = response.IdUsuario }, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            await _authService.ToggleUserStatusAsync(id);
            var nuevoEstado = user?.Activo == true ? "inactivo" : "activo";
            await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Usuarios", id, $"Usuario {user?.Email} cambiado a {nuevoEstado}");
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPatch("{id}/rol")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRolRequest request)
    {
        try
        {
            await _authService.UpdateUserRoleAsync(id, request.IdRol);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles.OrderBy(r => r.IdRol).ToListAsync();
        return Ok(roles.Select(r => new { r.IdRol, r.Nombre }));
    }
}

public class UpdateRolRequest
{
    public int IdRol { get; set; }
}
