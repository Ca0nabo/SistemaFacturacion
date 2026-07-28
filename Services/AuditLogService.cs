using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.Models;

namespace SistemaFacturacion.Services;

public interface IAuditLogService
{
    Task LogAsync(int idUsuario, string accion, string modulo, int? idRegistro = null, string? detalle = null);
    Task LogFromContextAsync(HttpContext httpContext, string accion, string modulo, int? idRegistro = null, string? detalle = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int idUsuario, string accion, string modulo, int? idRegistro = null, string? detalle = null)
    {
        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdUsuario = idUsuario,
            Accion = accion,
            Modulo = modulo,
            IdRegistro = idRegistro,
            Detalle = detalle,
            FechaRegistro = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task LogFromContextAsync(HttpContext httpContext, string accion, string modulo, int? idRegistro = null, string? detalle = null)
    {
        var idUsuarioClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idUsuarioClaim is null || !int.TryParse(idUsuarioClaim, out var idUsuario))
            return;
        await LogAsync(idUsuario, accion, modulo, idRegistro, detalle);
    }
}
