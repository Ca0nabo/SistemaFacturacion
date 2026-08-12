using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SistemaFacturacion.Data;
using SistemaFacturacion.Services;
using SistemaFacturacion.Security;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "Frontend"
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new ArgumentException("Jwt:Key es obligatorio. Configúralo mediante appsettings o variables de entorno.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaFacturacion";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaFacturacionApp";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new ArgumentException("ConnectionStrings:DefaultConnection es obligatorio.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.Catalog)
    {
        options.AddPolicy(permission.Key, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(permission.Key));
        });
    }
});
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5260", "https://localhost:7260"])
        .AllowAnyMethod()
        .AllowAnyHeader());
});

const string bearerSchemeName = "Bearer";
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HabitaCont",
        Version = "v2",
        Description = "API inmobiliaria para propiedades, contratos, facturación mensual, pagos, depósitos y acuerdos de pago."
    });
    c.AddSecurityDefinition(bearerSchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Ingresa Bearer seguido del token JWT."
    });
    c.DocumentFilter<SecurityDocumentFilter>();
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDgiiMockService, DgiiMockService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { mensaje = "Ocurrió un error interno. Revisa los registros del servidor." });
    });
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HabitaCont v2"));
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "HabitaCont", utc = DateTime.UtcNow })).AllowAnonymous();
app.MapFallbackToFile("index.html");

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Uploads"));

if (builder.Configuration.GetValue("Database:ApplyMigrations", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 3;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "No se pudo aplicar la migración. Reintento {Attempt}/{MaxRetries}.", attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    // Los datos iniciales usan Id explícitos (roles 1-6, usuario admin, etc.).
    // PostgreSQL no siempre adelanta automáticamente las secuencias cuando se insertan
    // esos valores desde una migración. Sin esta sincronización, el primer rol nuevo
    // puede intentar usar nuevamente IdRol = 1 y fallar por clave duplicada.
    await PostgresSequenceRepair.SynchronizeAsync(db, logger);

    // En producción, permite establecer el administrador inicial mediante secretos del entorno.
    var bootstrapAdminEmail = builder.Configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
    var bootstrapAdminPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (app.Environment.IsProduction() &&
        (string.IsNullOrWhiteSpace(bootstrapAdminEmail) || string.IsNullOrWhiteSpace(bootstrapAdminPassword)))
    {
        throw new InvalidOperationException(
            "En producción debes configurar BootstrapAdmin:Email y BootstrapAdmin:Password mediante secretos del entorno.");
    }

    if (!string.IsNullOrWhiteSpace(bootstrapAdminEmail) && !string.IsNullOrWhiteSpace(bootstrapAdminPassword))
    {
        if (bootstrapAdminPassword.Length < 8)
            throw new InvalidOperationException("BootstrapAdmin:Password debe tener al menos 8 caracteres.");

        var bootstrapAdminName = builder.Configuration["BootstrapAdmin:Name"]?.Trim();
        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == bootstrapAdminEmail);
        if (admin is null)
        {
            admin = new SistemaFacturacion.Models.User
            {
                Email = bootstrapAdminEmail,
                NombreCompleto = string.IsNullOrWhiteSpace(bootstrapAdminName) ? "Administrador HabitaCont" : bootstrapAdminName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(bootstrapAdminPassword),
                IdRol = 1,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            db.Usuarios.Add(admin);
        }
        else
        {
            admin.NombreCompleto = string.IsNullOrWhiteSpace(bootstrapAdminName) ? admin.NombreCompleto : bootstrapAdminName;
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(bootstrapAdminPassword);
            admin.IdRol = 1;
            admin.Activo = true;
        }
        await db.SaveChangesAsync();

        // Si el administrador sembrado para desarrollo es distinto, se desactiva en producción.
        if (app.Environment.IsProduction() && !bootstrapAdminEmail.Equals("admin@sistema.com", StringComparison.OrdinalIgnoreCase))
        {
            var demoAdmin = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@sistema.com");
            if (demoAdmin is not null && demoAdmin.IdUsuario != admin.IdUsuario && demoAdmin.Activo)
            {
                demoAdmin.Activo = false;
                await db.SaveChangesAsync();
            }
        }

        logger.LogInformation("Administrador de despliegue verificado mediante configuración segura.");
    }

    // Compatibilidad con facturas creadas antes de incorporar la condición CONTADO/CREDITO.
    // El campo TipoFactura existía en versiones anteriores con otro significado (Manual/AlquilerMensual).
    // Conservamos ese valor en OrigenFactura y clasificamos de forma segura las facturas históricas.
    // Migra en caliente los roles heredados al esquema de permisos granular sin cambiar el esquema de BD.
    await Permissions.NormalizeLegacyRolesAsync(db, logger);

    var facturasLegacy = await db.FacturasCabecera
        .Include(f => f.Pagos)
        .Where(f => f.TipoFactura != "CONTADO" && f.TipoFactura != "CREDITO")
        .ToListAsync();

    if (facturasLegacy.Count > 0)
    {
        var idsConAcuerdo = await db.AcuerdosPago
            .Where(a => a.IdFacturaOrigen.HasValue)
            .Select(a => a.IdFacturaOrigen!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var factura in facturasLegacy)
        {
            var valorAnterior = factura.TipoFactura;
            factura.OrigenFactura = string.IsNullOrWhiteSpace(valorAnterior) ? "AlquilerMensual" : valorAnterior;
            var pagado = factura.Pagos.Sum(p => p.Monto);
            var tuvoAbonoParcial = factura.Pagos.Count > 1 ||
                                    (pagado > 0 && pagado < factura.Total) ||
                                    factura.Estado is "PARCIAL" or "EN_ACUERDO" or "EN_PROCESO";
            factura.TipoFactura = tuvoAbonoParcial || idsConAcuerdo.Contains(factura.IdFactura)
                ? "CREDITO"
                : "CONTADO";

            if (factura.TipoFactura == "CREDITO" && factura.Estado == "PARCIAL")
                factura.Estado = "EN_PROCESO";
            else if (factura.TipoFactura == "CREDITO" && factura.Estado == "EMITIDA" && pagado <= 0)
                factura.Estado = "PENDIENTE";
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Se normalizaron {Cantidad} facturas históricas al esquema CONTADO/CREDITO.", facturasLegacy.Count);
    }

    var facturasSinOrigen = await db.FacturasCabecera
        .Where(f => f.OrigenFactura == null || f.OrigenFactura == string.Empty)
        .ToListAsync();
    if (facturasSinOrigen.Count > 0)
    {
        foreach (var factura in facturasSinOrigen)
            factura.OrigenFactura = string.IsNullOrWhiteSpace(factura.PeriodoFacturado) ? "Manual" : "AlquilerMensual";
        await db.SaveChangesAsync();
    }

    if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Demo:ResetAdminPassword", false))
    {
        var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@sistema.com");
        if (admin is not null)
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            await db.SaveChangesAsync();
            logger.LogWarning("Contraseña demo restablecida. No habilites Demo:ResetAdminPassword en producción.");
        }
    }
}

app.Run();
