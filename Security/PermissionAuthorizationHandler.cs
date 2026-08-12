using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;

namespace SistemaFacturacion.Security;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _db;

    public PermissionAuthorizationHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId)) return;

        var user = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == userId);

        if (user is null || !user.Activo) return;

        var permissions = Permissions.Parse(user.Rol.Permisos);
        if (permissions.Contains(Permissions.Todo) || permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
