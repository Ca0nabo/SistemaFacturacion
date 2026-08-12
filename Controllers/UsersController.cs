using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Auth;
using SistemaFacturacion.Services;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [Authorize(Policy = Permissions.UsuariosVer)]
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
    [Authorize(Policy = Permissions.UsuariosVer)]
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
    [Authorize(Policy = Permissions.UsuariosGestionar)]
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

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.UsuariosGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateUserRequest request)
    {
        var user = await _context.Usuarios.FindAsync(id);
        if (user is null)
            return NotFound(new { mensaje = "Usuario no encontrado." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.Email == email && u.IdUsuario != id))
            return Conflict(new { mensaje = "El correo ya está en uso por otro usuario." });
        if (!await _context.Roles.AnyAsync(r => r.IdRol == request.IdRol))
            return BadRequest(new { mensaje = "El rol seleccionado no existe." });
        if (user.IdRol == 1 && request.IdRol != 1 && await _context.Usuarios.CountAsync(u => u.IdRol == 1 && u.Activo) <= 1)
            return BadRequest(new { mensaje = "Debe existir al menos un Administrador activo en el sistema." });

        user.NombreCompleto = request.NombreCompleto.Trim();
        user.Email = email;
        user.IdRol = request.IdRol;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Usuarios", user.IdUsuario, $"Usuario {user.Email} actualizado");
        return NoContent();
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Policy = Permissions.UsuariosGestionar)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var currentId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : 0;
            if (id == currentId)
                return BadRequest(new { mensaje = "No puedes desactivar tu propia cuenta mientras tienes una sesión activa." });

            var user = await _authService.GetUserByIdAsync(id);
            if (user?.IdRol == 1 && user.Activo && await _context.Usuarios.CountAsync(u => u.IdRol == 1 && u.Activo) <= 1)
                return BadRequest(new { mensaje = "No se puede desactivar al último Administrador activo." });
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
    [Authorize(Policy = Permissions.UsuariosGestionar)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRolRequest request)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user?.IdRol == 1 && request.IdRol != 1 && await _context.Usuarios.CountAsync(u => u.IdRol == 1 && u.Activo) <= 1)
                return BadRequest(new { mensaje = "Debe existir al menos un Administrador activo en el sistema." });

            await _authService.UpdateUserRoleAsync(id, request.IdRol);
            await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Usuarios", id, $"Rol del usuario {user?.Email} actualizado a IdRol {request.IdRol}");
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("roles")]
    [Authorize(Policy = Permissions.UsuariosGestionar)]
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
