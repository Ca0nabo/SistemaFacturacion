using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Models;

namespace SistemaFacturacion.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Usuarios => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ParametrosEmpresa> ParametrosEmpresa => Set<ParametrosEmpresa>();
    public DbSet<Entidad> Entidades => Set<Entidad>();
    public DbSet<FacturaCabecera> FacturasCabecera => Set<FacturaCabecera>();
    public DbSet<FacturaDetalle> FacturasDetalle => Set<FacturaDetalle>();
    public DbSet<MovimientosCx> MovimientosCx => Set<MovimientosCx>();
    public DbSet<CatalogoCuentas> CatalogoCuentas => Set<CatalogoCuentas>();
    public DbSet<AsientoContable> AsientosContables => Set<AsientoContable>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<AuditoriaLog> AuditoriaLogs => Set<AuditoriaLog>();
    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<Unidad> Unidades => Set<Unidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.IdUsuario);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.PasswordHash).HasMaxLength(200);
            e.Property(x => x.NombreCompleto).HasMaxLength(100);
            e.Property(x => x.FechaCreacion).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.IdRol);
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(50);
            e.Property(x => x.Permisos).HasMaxLength(500);
        });

        modelBuilder.Entity<ParametrosEmpresa>(e =>
        {
            e.HasKey(x => x.IdParametro);
            e.Property(x => x.NombreEmpresa).HasMaxLength(200);
            e.Property(x => x.SecuenciaEmpresa).HasMaxLength(20);
            e.Property(x => x.SecuenciaFiscalECF).HasMaxLength(20);
            e.Property(x => x.PorcentajeITBIS).HasColumnType("decimal(5,4)");
        });

        modelBuilder.Entity<Entidad>(e =>
        {
            e.HasKey(x => x.IdEntidad);
            e.HasIndex(x => x.RncCedula).IsUnique();
            e.Property(x => x.Tipo).HasMaxLength(20);
            e.Property(x => x.RncCedula).HasMaxLength(20);
            e.Property(x => x.RazonSocial).HasMaxLength(200);
        });

        modelBuilder.Entity<FacturaCabecera>(e =>
        {
            e.HasKey(x => x.IdFactura);
            e.HasIndex(x => x.NumeroECF).IsUnique();
            e.Property(x => x.FechaEmision).HasDefaultValueSql("NOW()");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Itbis).HasColumnType("decimal(18,2)");
            e.Property(x => x.NumeroECF).HasMaxLength(50);
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.FirmaDGII).HasMaxLength(100);
        });

        modelBuilder.Entity<FacturaDetalle>(e =>
        {
            e.HasKey(x => x.IdDetalle);
            e.Property(x => x.DescripcionItem).HasMaxLength(500);
            e.Property(x => x.Cantidad).HasColumnType("decimal(18,2)");
            e.Property(x => x.Precio).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<MovimientosCx>(e =>
        {
            e.HasKey(x => x.IdMovimiento);
            e.Property(x => x.Tipo).HasMaxLength(3).IsRequired();
            e.Property(x => x.MontoOriginal).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoPendiente).HasColumnType("decimal(18,2)");
            e.Property(x => x.CategoriaGasto).HasMaxLength(50);
            e.Property(x => x.ArchivoEvidencia).HasMaxLength(500);
        });

        modelBuilder.Entity<CatalogoCuentas>(e =>
        {
            e.HasKey(x => x.IdCuentaContable);
            e.Property(x => x.NombreCuenta).HasMaxLength(200);
        });

        modelBuilder.Entity<AsientoContable>(e =>
        {
            e.HasKey(x => x.IdAsiento);
            e.Property(x => x.MontoDebito).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoCredito).HasColumnType("decimal(18,2)");
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.FechaRegistro).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Propiedad>(e =>
        {
            e.HasKey(x => x.IdPropiedad);
            e.Property(x => x.TipoPropiedad).HasMaxLength(30);
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.Property(x => x.Sector).HasMaxLength(100);
            e.Property(x => x.Ciudad).HasMaxLength(100);
            e.Property(x => x.MetrosCuadrados).HasColumnType("decimal(10,2)");
            e.Property(x => x.Estado).HasMaxLength(30);
        });

        modelBuilder.Entity<Unidad>(e =>
        {
            e.HasKey(x => x.IdUnidad);
            e.Property(x => x.Codigo).HasMaxLength(20);
            e.Property(x => x.Piso).HasMaxLength(20);
            e.Property(x => x.MetrosCuadrados).HasColumnType("decimal(10,2)");
            e.Property(x => x.Estado).HasMaxLength(30);
            e.HasIndex(x => new { x.IdPropiedad, x.Codigo }).IsUnique();
        });

        modelBuilder.Entity<Contrato>(e =>
        {
            e.HasKey(x => x.IdContrato);
            e.Property(x => x.TipoContrato).HasMaxLength(30);
            e.Property(x => x.Condiciones).HasMaxLength(500);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoMantenimiento).HasColumnType("decimal(18,2)");
            e.Property(x => x.Deposito).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasMaxLength(20);
        });

        modelBuilder.Entity<AuditoriaLog>(e =>
        {
            e.HasKey(x => x.IdLog);
            e.Property(x => x.Accion).HasMaxLength(50);
            e.Property(x => x.Modulo).HasMaxLength(50);
            e.Property(x => x.Detalle).HasMaxLength(500);
            e.Property(x => x.FechaRegistro).HasDefaultValueSql("NOW()");
        });

        // Relationships
        modelBuilder.Entity<User>().HasOne(e => e.Rol).WithMany(r => r.Usuarios).HasForeignKey(e => e.IdRol).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Entidad).WithMany(e => e.Facturas).HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FacturaDetalle>().HasOne(e => e.Factura).WithMany(f => f.Detalles).HasForeignKey(e => e.IdFactura).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MovimientosCx>().HasOne(e => e.Factura).WithMany(f => f.Movimientos).HasForeignKey(e => e.IdFactura).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Contrato>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoContable>().HasOne(e => e.FacturaReferencia).WithMany().HasForeignKey(e => e.IdFacturaReferencia).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AsientoContable>().HasOne(e => e.CuentaContable).WithMany(c => c.Asientos).HasForeignKey(e => e.IdCuentaContable).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AuditoriaLog>().HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.IdUsuario).OnDelete(DeleteBehavior.Restrict);

        // Propiedad relationships
        modelBuilder.Entity<Propiedad>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Unidad>().HasOne(e => e.Propiedad).WithMany(p => p.Unidades).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Contrato>().HasOne(e => e.Propiedad).WithMany(p => p.Contratos).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Contrato>().HasOne(e => e.Unidad).WithMany(u => u.Contratos).HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Propiedad).WithMany(p => p.Facturas).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Unidad).WithMany(u => u.Facturas).HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientosCx>().HasOne(e => e.Propiedad).WithMany().HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientosCx>().HasOne(e => e.Unidad).WithMany().HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);

        // Seed data
        modelBuilder.Entity<Role>().HasData(
            new Role { IdRol = 1, Nombre = "Administrador", Permisos = "TODO" },
            new Role { IdRol = 2, Nombre = "Contador", Permisos = "FACTURAS,REPORTES,MOVIMIENTOS" },
            new Role { IdRol = 3, Nombre = "Encargado de facturación", Permisos = "FACTURAS,ENTIDADES" },
            new Role { IdRol = 4, Nombre = "Gerente financiero", Permisos = "REPORTES,CONTRATOS,MOVIMIENTOS" },
            new Role { IdRol = 5, Nombre = "Cliente", Permisos = "CONSULTA" },
            new Role { IdRol = 6, Nombre = "Proveedor", Permisos = "CONSULTA" }
        );

        modelBuilder.Entity<CatalogoCuentas>().HasData(
            new CatalogoCuentas { IdCuentaContable = 1, NombreCuenta = "Caja" },
            new CatalogoCuentas { IdCuentaContable = 2, NombreCuenta = "Banco" },
            new CatalogoCuentas { IdCuentaContable = 3, NombreCuenta = "Cuentas por Cobrar" },
            new CatalogoCuentas { IdCuentaContable = 4, NombreCuenta = "Cuentas por Pagar" },
            new CatalogoCuentas { IdCuentaContable = 5, NombreCuenta = "Ingresos por Ventas" },
            new CatalogoCuentas { IdCuentaContable = 6, NombreCuenta = "ITBIS por Pagar" }
        );

        modelBuilder.Entity<ParametrosEmpresa>().HasData(
            new ParametrosEmpresa { IdParametro = 1, NombreEmpresa = "Mi Empresa SRL", SecuenciaEmpresa = "001", SecuenciaFiscalECF = "ECF001", PorcentajeITBIS = 0.18m }
        );

        modelBuilder.Entity<User>().HasData(
            new User { IdUsuario = 1, Email = "admin@sistema.com", PasswordHash = "$2a$11$4bEImu8PNaPhgHS2iQM8YOg1gubP0uFTqDOxh8QgedWuMvs1WYMma", NombreCompleto = "Administrador del Sistema", IdRol = 1, Activo = true, FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );


    }
}
