using Microsoft.EntityFrameworkCore;

namespace SistemaFacturacion.Data;

/// <summary>
/// Sincroniza las secuencias de PostgreSQL de tablas que contienen registros
/// sembrados con identificadores explícitos. Esto evita que el primer INSERT
/// intente reutilizar un Id ya existente (por ejemplo IdRol = 1).
/// </summary>
public static class PostgresSequenceRepair
{
    private static readonly string[] Statements =
    [
        """
        SELECT setval(
            pg_get_serial_sequence('"Roles"', 'IdRol'),
            GREATEST(COALESCE(MAX("IdRol"), 1), 1),
            COUNT(*) > 0
        )
        FROM "Roles";
        """,
        """
        SELECT setval(
            pg_get_serial_sequence('"Usuarios"', 'IdUsuario'),
            GREATEST(COALESCE(MAX("IdUsuario"), 1), 1),
            COUNT(*) > 0
        )
        FROM "Usuarios";
        """,
        """
        SELECT setval(
            pg_get_serial_sequence('"CatalogoCuentas"', 'IdCuentaContable'),
            GREATEST(COALESCE(MAX("IdCuentaContable"), 1), 1),
            COUNT(*) > 0
        )
        FROM "CatalogoCuentas";
        """,
        """
        SELECT setval(
            pg_get_serial_sequence('"ParametrosEmpresa"', 'IdParametro'),
            GREATEST(COALESCE(MAX("IdParametro"), 1), 1),
            COUNT(*) > 0
        )
        FROM "ParametrosEmpresa";
        """
    ];

    public static async Task SynchronizeAsync(ApplicationDbContext db, ILogger logger)
    {
        foreach (var statement in Statements)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(statement);
            }
            catch (Exception ex)
            {
                // No impedimos el arranque por una tarea de mantenimiento,
                // pero dejamos el diagnóstico registrado en consola/logs.
                logger.LogWarning(ex, "No se pudo sincronizar una secuencia de PostgreSQL.");
            }
        }

        logger.LogInformation("Secuencias de PostgreSQL verificadas para tablas con datos iniciales.");
    }
}
