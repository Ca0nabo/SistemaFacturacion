using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;

namespace SistemaFacturacion.Security;

public static class Permissions
{
    public const string Todo = "TODO";

    public const string DashboardVer = "DASHBOARD.VER";
    public const string EntidadesVer = "ENTIDADES.VER";
    public const string EntidadesGestionar = "ENTIDADES.GESTIONAR";
    public const string PropiedadesVer = "PROPIEDADES.VER";
    public const string PropiedadesGestionar = "PROPIEDADES.GESTIONAR";
    public const string ContratosVer = "CONTRATOS.VER";
    public const string ContratosGestionar = "CONTRATOS.GESTIONAR";
    public const string FacturasVer = "FACTURAS.VER";
    public const string FacturasCrear = "FACTURAS.CREAR";
    public const string FacturasPagar = "FACTURAS.PAGAR";
    public const string FacturasAnular = "FACTURAS.ANULAR";
    public const string DepositosVer = "DEPOSITOS.VER";
    public const string DepositosGestionar = "DEPOSITOS.GESTIONAR";
    public const string AcuerdosVer = "ACUERDOS.VER";
    public const string AcuerdosGestionar = "ACUERDOS.GESTIONAR";
    public const string AcuerdosPagar = "ACUERDOS.PAGAR";
    public const string MovimientosVer = "MOVIMIENTOS.VER";
    public const string GastosGestionar = "GASTOS.GESTIONAR";
    public const string ReportesVer = "REPORTES.VER";
    public const string AlertasVer = "ALERTAS.VER";
    public const string UsuariosVer = "USUARIOS.VER";
    public const string UsuariosGestionar = "USUARIOS.GESTIONAR";
    public const string RolesVer = "ROLES.VER";
    public const string RolesGestionar = "ROLES.GESTIONAR";
    public const string AuditoriaVer = "AUDITORIA.VER";

    public static readonly IReadOnlyList<PermissionDefinition> Catalog =
    [
        new(DashboardVer, "Dashboard", "Consultar", "Ver indicadores y resumen operativo."),
        new(EntidadesVer, "Personas y empresas", "Consultar", "Consultar inquilinos, propietarios y proveedores."),
        new(EntidadesGestionar, "Personas y empresas", "Gestionar", "Crear, editar y activar/desactivar entidades."),
        new(PropiedadesVer, "Propiedades", "Consultar", "Consultar propiedades y unidades."),
        new(PropiedadesGestionar, "Propiedades", "Gestionar", "Crear, editar y cambiar propiedades o unidades."),
        new(ContratosVer, "Contratos", "Consultar", "Consultar contratos y sus resúmenes."),
        new(ContratosGestionar, "Contratos", "Gestionar", "Crear, editar y cambiar el estado de contratos."),
        new(FacturasVer, "Facturación", "Consultar", "Consultar facturas, cuotas y saldos."),
        new(FacturasCrear, "Facturación", "Crear", "Generar facturas individuales y mensuales."),
        new(FacturasPagar, "Facturación", "Registrar pagos", "Registrar cobros de facturas."),
        new(FacturasAnular, "Facturación", "Anular", "Anular facturas cuando las reglas de negocio lo permitan."),
        new(DepositosVer, "Depósitos", "Consultar", "Consultar depósitos de garantía."),
        new(DepositosGestionar, "Depósitos", "Gestionar", "Crear, editar y desactivar depósitos."),
        new(AcuerdosVer, "Acuerdos de pago", "Consultar", "Consultar acuerdos y sus cuotas."),
        new(AcuerdosGestionar, "Acuerdos de pago", "Gestionar", "Crear, editar o cancelar acuerdos."),
        new(AcuerdosPagar, "Acuerdos de pago", "Registrar pagos", "Registrar pagos a cuotas de acuerdos."),
        new(MovimientosVer, "Movimientos", "Consultar", "Consultar estados de cuenta y saldos."),
        new(GastosGestionar, "Gastos", "Gestionar", "Registrar y pagar cuentas por pagar."),
        new(ReportesVer, "Reportes", "Consultar", "Consultar reportes financieros y operativos."),
        new(AlertasVer, "Alertas", "Consultar", "Consultar alertas de vencimientos y morosidad."),
        new(UsuariosVer, "Seguridad", "Consultar usuarios", "Consultar usuarios y su estado."),
        new(UsuariosGestionar, "Seguridad", "Gestionar usuarios", "Crear, editar, activar y asignar roles a usuarios."),
        new(RolesVer, "Seguridad", "Consultar roles", "Consultar roles y matriz de permisos."),
        new(RolesGestionar, "Seguridad", "Gestionar roles", "Crear y modificar roles y permisos."),
        new(AuditoriaVer, "Seguridad", "Auditoría", "Consultar trazabilidad y bitácora de operaciones.")
    ];

    public static readonly IReadOnlySet<string> Keys = Catalog.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static HashSet<string> Parse(string? permisos)
    {
        if (string.IsNullOrWhiteSpace(permisos)) return new(StringComparer.OrdinalIgnoreCase);
        return permisos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p == Todo || Keys.Contains(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static string Serialize(IEnumerable<string>? permisos)
    {
        if (permisos is null) return string.Empty;
        var normalized = permisos
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToUpperInvariant())
            .Where(p => p == Todo || Keys.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalized.Contains(Todo, StringComparer.OrdinalIgnoreCase)) return Todo;
        return string.Join(',', normalized.OrderBy(p => p));
    }

    public static IReadOnlyList<string> Expand(string? permisos)
    {
        var parsed = Parse(permisos);
        return parsed.Contains(Todo)
            ? Catalog.Select(x => x.Key).ToList()
            : Catalog.Where(x => parsed.Contains(x.Key)).Select(x => x.Key).ToList();
    }

    public static async Task NormalizeLegacyRolesAsync(ApplicationDbContext db, ILogger logger)
    {
        var legacyDefaults = new Dictionary<string, (string Legacy, string NewValue)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Administrador"] = (Todo, Todo),
            ["Contador"] = ("FACTURAS,REPORTES,MOVIMIENTOS,PAGOS,DEPOSITOS,ACUERDOS", Serialize([
                DashboardVer, EntidadesVer, PropiedadesVer, ContratosVer,
                FacturasVer, FacturasCrear, FacturasPagar, DepositosVer, DepositosGestionar,
                AcuerdosVer, AcuerdosGestionar, AcuerdosPagar, MovimientosVer, GastosGestionar,
                ReportesVer, AlertasVer
            ])),
            ["Encargado de facturación"] = ("FACTURAS,ENTIDADES,CONTRATOS", Serialize([
                DashboardVer, EntidadesVer, EntidadesGestionar, PropiedadesVer,
                ContratosVer, ContratosGestionar, FacturasVer, FacturasCrear, FacturasPagar,
                FacturasAnular, DepositosVer, DepositosGestionar, AlertasVer
            ])),
            ["Gerente financiero"] = ("REPORTES,CONTRATOS,MOVIMIENTOS,DEPOSITOS,ACUERDOS", Serialize([
                DashboardVer, EntidadesVer, PropiedadesVer, ContratosVer, FacturasVer,
                DepositosVer, AcuerdosVer, MovimientosVer, ReportesVer, AlertasVer
            ])),
            ["Cliente"] = ("CONSULTA", string.Empty),
            ["Proveedor"] = ("CONSULTA", string.Empty)
        };

        var roles = await db.Roles.ToListAsync();
        var changed = false;
        foreach (var role in roles)
        {
            if (!legacyDefaults.TryGetValue(role.Nombre, out var rule)) continue;
            var actual = role.Permisos?.Trim() ?? string.Empty;
            if (role.Nombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                if (!actual.Equals(Todo, StringComparison.OrdinalIgnoreCase))
                {
                    role.Permisos = Todo;
                    changed = true;
                }
                continue;
            }

            if (string.IsNullOrWhiteSpace(actual) || actual.Equals(rule.Legacy, StringComparison.OrdinalIgnoreCase))
            {
                role.Permisos = rule.NewValue;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Permisos heredados de roles normalizados al esquema RBAC de HabitaCont.");
        }
    }
}

public sealed record PermissionDefinition(string Key, string Module, string Action, string Description);
