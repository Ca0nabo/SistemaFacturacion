using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.Models;

namespace SistemaFacturacion.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuditActionFilter : Attribute, IAsyncActionFilter
{
    private static readonly string[] IdPropertyNames = ["IdFactura", "IdContrato", "IdEntidad", "IdMovimiento", "IdAsiento"];
    private readonly string _modulo;

    public AuditActionFilter(string modulo) { _modulo = modulo; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();
        object? value = resultContext.Result switch
        {
            CreatedAtActionResult c => c.Value,
            ObjectResult o => o.Value,
            _ => null
        };
        if (value is null) return;

        var statusCode = (resultContext.Result as ObjectResult)?.StatusCode ?? (resultContext.Result as CreatedAtActionResult)?.StatusCode ?? 200;
        if (statusCode < 200 || statusCode > 299) return;

        var httpContext = context.HttpContext;
        var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var idUsuarioClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idUsuarioClaim is null || !int.TryParse(idUsuarioClaim, out var idUsuario)) return;

        var accion = httpContext.Request.Method switch { "POST" => "CREAR", "PUT" => "EDITAR", "DELETE" => "ANULAR", _ => null };
        if (accion is null) return;

        int? idRegistro = null;
        var type = value.GetType();
        foreach (var propName in IdPropertyNames)
        {
            var prop = type.GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop is not null) { idRegistro = prop.GetValue(value) as int?; if (idRegistro.HasValue) break; }
        }

        dbContext.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdUsuario = idUsuario,
            Accion = accion,
            Modulo = _modulo,
            IdRegistro = idRegistro,
            FechaRegistro = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}
