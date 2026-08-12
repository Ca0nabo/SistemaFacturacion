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
    public DbSet<MovimientoCuenta> MovimientosCuenta => Set<MovimientoCuenta>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<DepositoGarantia> DepositosGarantia => Set<DepositoGarantia>();
    public DbSet<AcuerdoPago> AcuerdosPago => Set<AcuerdoPago>();
    public DbSet<CuotaAcuerdoPago> CuotasAcuerdoPago => Set<CuotaAcuerdoPago>();
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

        modelBuilder.Entity<Propiedad>(e =>
        {
            e.HasKey(x => x.IdPropiedad);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(30);
            e.Property(x => x.TipoPropiedad).HasMaxLength(30);
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.Property(x => x.Sector).HasMaxLength(100);
            e.Property(x => x.Ciudad).HasMaxLength(100);
            e.Property(x => x.MetrosCuadrados).HasColumnType("decimal(10,2)");
            e.Property(x => x.CanonMensualSugerido).HasColumnType("decimal(18,2)");
            e.Property(x => x.MantenimientoMensualSugerido).HasColumnType("decimal(18,2)");
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
            e.HasIndex(x => new { x.IdPropiedad, x.IdUnidad, x.Estado });
        });

        modelBuilder.Entity<FacturaCabecera>(e =>
        {
            e.HasKey(x => x.IdFactura);
            e.HasIndex(x => x.NumeroECF).IsUnique();
            e.HasIndex(x => new { x.IdContrato, x.PeriodoFacturado, x.OrigenFactura });
            e.Property(x => x.FechaEmision).HasDefaultValueSql("NOW()");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Itbis).HasColumnType("decimal(18,2)");
            e.Property(x => x.NumeroECF).HasMaxLength(50);
            e.Property(x => x.TipoFactura).HasMaxLength(15).IsRequired();
            e.Property(x => x.OrigenFactura).HasMaxLength(30).IsRequired();
            e.Property(x => x.PeriodoFacturado).HasMaxLength(7);
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

        modelBuilder.Entity<Pago>(e =>
        {
            e.HasKey(x => x.IdPago);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.FechaPago).HasDefaultValueSql("NOW()");
            e.Property(x => x.MetodoPago).HasMaxLength(30);
            e.Property(x => x.Referencia).HasMaxLength(100);
            e.Property(x => x.Notas).HasMaxLength(500);
        });

        modelBuilder.Entity<MovimientoCuenta>(e =>
        {
            e.HasKey(x => x.IdMovimientoCuenta);
            e.Property(x => x.Fecha).HasDefaultValueSql("NOW()");
            e.Property(x => x.TipoMovimiento).HasMaxLength(30);
            e.Property(x => x.Concepto).HasMaxLength(300);
            e.Property(x => x.Referencia).HasMaxLength(100);
            e.Property(x => x.Debito).HasColumnType("decimal(18,2)");
            e.Property(x => x.Credito).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.IdEntidad, x.IdPropiedad, x.Fecha });
        });

        modelBuilder.Entity<DepositoGarantia>(e =>
        {
            e.HasKey(x => x.IdDeposito);
            e.Property(x => x.MontoRequerido).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoRecibido).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.MetodoPago).HasMaxLength(30);
            e.Property(x => x.Referencia).HasMaxLength(100);
            e.Property(x => x.Observaciones).HasMaxLength(500);
        });

        modelBuilder.Entity<AcuerdoPago>(e =>
        {
            e.HasKey(x => x.IdAcuerdo);
            e.Property(x => x.MontoOriginal).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoAcordado).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoCuota).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasMaxLength(20);
            e.Property(x => x.Observaciones).HasMaxLength(500);
            e.Property(x => x.FechaCreacion).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<CuotaAcuerdoPago>(e =>
        {
            e.HasKey(x => x.IdCuotaAcuerdo);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoPagado).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasMaxLength(20);
            e.HasIndex(x => new { x.IdAcuerdo, x.NumeroCuota }).IsUnique();
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

        modelBuilder.Entity<AuditoriaLog>(e =>
        {
            e.HasKey(x => x.IdLog);
            e.Property(x => x.Accion).HasMaxLength(50);
            e.Property(x => x.Modulo).HasMaxLength(50);
            e.Property(x => x.Detalle).HasMaxLength(500);
            e.Property(x => x.FechaRegistro).HasDefaultValueSql("NOW()");
        });

        // Relaciones principales
        modelBuilder.Entity<User>().HasOne(e => e.Rol).WithMany(r => r.Usuarios).HasForeignKey(e => e.IdRol).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Propiedad>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Unidad>().HasOne(e => e.Propiedad).WithMany(p => p.Unidades).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Contrato>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contrato>().HasOne(e => e.Propiedad).WithMany(p => p.Contratos).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Contrato>().HasOne(e => e.Unidad).WithMany(u => u.Contratos).HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Entidad).WithMany(e => e.Facturas).HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Contrato).WithMany(c => c.Facturas).HasForeignKey(e => e.IdContrato).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Propiedad).WithMany(p => p.Facturas).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FacturaCabecera>().HasOne(e => e.Unidad).WithMany(u => u.Facturas).HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FacturaDetalle>().HasOne(e => e.Factura).WithMany(f => f.Detalles).HasForeignKey(e => e.IdFactura).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MovimientosCx>().HasOne(e => e.Factura).WithMany(f => f.Movimientos).HasForeignKey(e => e.IdFactura).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MovimientosCx>().HasOne(e => e.Propiedad).WithMany().HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientosCx>().HasOne(e => e.Unidad).WithMany().HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Pago>().HasOne(e => e.Factura).WithMany(f => f.Pagos).HasForeignKey(e => e.IdFactura).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pago>().HasOne(e => e.Contrato).WithMany(c => c.Pagos).HasForeignKey(e => e.IdContrato).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Pago>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pago>().HasOne(e => e.Propiedad).WithMany().HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Pago>().HasOne(e => e.Unidad).WithMany().HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MovimientoCuenta>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovimientoCuenta>().HasOne(e => e.Propiedad).WithMany(p => p.MovimientosCuenta).HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientoCuenta>().HasOne(e => e.Unidad).WithMany().HasForeignKey(e => e.IdUnidad).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientoCuenta>().HasOne(e => e.Contrato).WithMany().HasForeignKey(e => e.IdContrato).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientoCuenta>().HasOne(e => e.Factura).WithMany().HasForeignKey(e => e.IdFactura).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MovimientoCuenta>().HasOne(e => e.Pago).WithMany().HasForeignKey(e => e.IdPago).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DepositoGarantia>().HasOne(e => e.Contrato).WithMany(c => c.Depositos).HasForeignKey(e => e.IdContrato).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AcuerdoPago>().HasOne(e => e.Contrato).WithMany(c => c.AcuerdosPago).HasForeignKey(e => e.IdContrato).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AcuerdoPago>().HasOne(e => e.Entidad).WithMany().HasForeignKey(e => e.IdEntidad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AcuerdoPago>().HasOne(e => e.Propiedad).WithMany().HasForeignKey(e => e.IdPropiedad).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AcuerdoPago>().HasOne(e => e.FacturaOrigen).WithMany().HasForeignKey(e => e.IdFacturaOrigen).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CuotaAcuerdoPago>().HasOne(e => e.Acuerdo).WithMany(a => a.Cuotas).HasForeignKey(e => e.IdAcuerdo).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AsientoContable>().HasOne(e => e.FacturaReferencia).WithMany().HasForeignKey(e => e.IdFacturaReferencia).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AsientoContable>().HasOne(e => e.CuentaContable).WithMany(c => c.Asientos).HasForeignKey(e => e.IdCuentaContable).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AuditoriaLog>().HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.IdUsuario).OnDelete(DeleteBehavior.Restrict);

        // Datos iniciales
        modelBuilder.Entity<Role>().HasData(
            new Role { IdRol = 1, Nombre = "Administrador", Permisos = "TODO" },
            new Role { IdRol = 2, Nombre = "Contador", Permisos = "FACTURAS,REPORTES,MOVIMIENTOS,PAGOS,DEPOSITOS,ACUERDOS" },
            new Role { IdRol = 3, Nombre = "Encargado de facturación", Permisos = "FACTURAS,ENTIDADES,CONTRATOS" },
            new Role { IdRol = 4, Nombre = "Gerente financiero", Permisos = "REPORTES,CONTRATOS,MOVIMIENTOS,DEPOSITOS,ACUERDOS" },
            new Role { IdRol = 5, Nombre = "Cliente", Permisos = "CONSULTA" },
            new Role { IdRol = 6, Nombre = "Proveedor", Permisos = "CONSULTA" }
        );

        modelBuilder.Entity<CatalogoCuentas>().HasData(
            new CatalogoCuentas { IdCuentaContable = 1, NombreCuenta = "Caja" },
            new CatalogoCuentas { IdCuentaContable = 2, NombreCuenta = "Banco" },
            new CatalogoCuentas { IdCuentaContable = 3, NombreCuenta = "Cuentas por Cobrar" },
            new CatalogoCuentas { IdCuentaContable = 4, NombreCuenta = "Cuentas por Pagar" },
            new CatalogoCuentas { IdCuentaContable = 5, NombreCuenta = "Ingresos por Alquiler" },
            new CatalogoCuentas { IdCuentaContable = 6, NombreCuenta = "ITBIS por Pagar" }
        );

        modelBuilder.Entity<ParametrosEmpresa>().HasData(
            new ParametrosEmpresa { IdParametro = 1, NombreEmpresa = "HabitaCont SRL", SecuenciaEmpresa = "001", SecuenciaFiscalECF = "FAC", PorcentajeITBIS = 0.18m }
        );

        modelBuilder.Entity<User>().HasData(
            new User
            {
                IdUsuario = 1,
                Email = "admin@sistema.com",
                PasswordHash = "$2a$11$4bEImu8PNaPhgHS2iQM8YOg1gubP0uFTqDOxh8QgedWuMvs1WYMma",
                NombreCompleto = "Administrador del Sistema",
                IdRol = 1,
                Activo = true,
                FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
