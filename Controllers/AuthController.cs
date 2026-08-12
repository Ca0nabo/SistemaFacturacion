using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacion.DTOs.Auth;
using SistemaFacturacion.Services;
using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLog;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IAuditLogService auditLog,
        IConfiguration configuration)
    {
        _authService = authService;
        _auditLog = auditLog;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("auto-login")]
    public async Task<IActionResult> AutoLogin()
    {
        var enabled =
            bool.TryParse(
                _configuration["Demo:AutoLogin"],
                out var autoLoginEnabled)
            && autoLoginEnabled;

        if (!enabled)
        {
            return NotFound(new
            {
                mensaje = "El acceso automático está deshabilitado."
            });
        }

        var email = _configuration["Demo:AutoLoginEmail"];

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                mensaje = "Demo:AutoLoginEmail no está configurado."
            });
        }

        try
        {
            var response =
                await _authService.AutoLoginAsync(email);

            await _auditLog.LogAsync(
                response.IdUsuario,
                "INICIAR_SESION_AUTO",
                "Auth",
                null,
                $"Acceso automático del usuario {email}"
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        try
        {
            var response =
                await _authService.LoginAsync(request);

            await _auditLog.LogAsync(
                response.IdUsuario,
                "INICIAR_SESION",
                "Auth",
                null,
                $"Usuario {request.Email} inició sesión"
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    [HttpPost("register")]
    [Authorize(Policy = Permissions.UsuariosGestionar)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        try
        {
            var response =
                await _authService.RegisterAsync(request);

            await _auditLog.LogFromContextAsync(
                HttpContext,
                "CREAR",
                "Usuarios",
                response.IdUsuario,
                $"Usuario {request.Email} registrado"
            );

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword(
        [FromBody] CambiarPasswordRequest request)
    {
        try
        {
            var idUsuario = int.Parse(
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )!.Value
            );

            await _authService.CambiarPasswordAsync(
                idUsuario,
                request.PasswordActual,
                request.NuevaPassword
            );

            await _auditLog.LogFromContextAsync(
                HttpContext,
                "EDITAR",
                "Auth",
                idUsuario,
                "Contraseña cambiada"
            );

            return Ok(new
            {
                mensaje = "Contraseña actualizada exitosamente."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPut("perfil")]
    public async Task<IActionResult> ActualizarPerfil(
        [FromBody] UpdateUserRequest request)
    {
        try
        {
            var idUsuario = int.Parse(
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )!.Value
            );

            var response =
                await _authService.UpdateUserProfileAsync(
                    idUsuario,
                    request
                );

            await _auditLog.LogFromContextAsync(
                HttpContext,
                "EDITAR",
                "Auth",
                idUsuario,
                "Perfil actualizado"
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                mensaje = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensaje = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var idUsuario = int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value
        );

        var user =
            await _authService.GetUserByIdAsync(idUsuario);

        if (user is null)
        {
            return NotFound(new
            {
                mensaje = "Usuario no encontrado."
            });
        }

        if (!user.Activo)
        {
            return Unauthorized(new
            {
                mensaje = "La cuenta está desactivada."
            });
        }

        return Ok(new
        {
            user.IdUsuario,
            user.Email,
            user.NombreCompleto,
            user.IdRol,
            Rol = user.Rol.Nombre,
            Permisos = Permissions.Expand(
                user.Rol.Permisos
            ),
            user.Activo
        });
    }
}