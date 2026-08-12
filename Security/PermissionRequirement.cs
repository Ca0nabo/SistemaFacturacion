using Microsoft.AspNetCore.Authorization;

namespace SistemaFacturacion.Security;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
