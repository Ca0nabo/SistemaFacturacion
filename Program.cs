using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SistemaFacturacion.Data;
using SistemaFacturacion.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate JWT key at startup
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new ArgumentException("Jwt:Key es obligatorio en appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaFacturacion";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaFacturacionApp";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

const string bearerSchemeName = "Bearer";
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HabitaCont",
        Version = "v1",
        Description = "API de gestión inmobiliaria HabitaCont"
    });
    c.AddSecurityDefinition(bearerSchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Ingresa 'Bearer ' seguido del token. Ej: Bearer eyJhbGci..."
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
        await context.Response.WriteAsJsonAsync(new { mensaje = "Error interno del servidor." });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Facturación v1");
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure Uploads directory exists
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Uploads"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var maxRetries = 3;
    var retryDelay = TimeSpan.FromSeconds(5);

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Intento {Attempt}/{MaxRetries} de conexión a BD falló. Reintentando en {Delay}...", attempt, maxRetries, retryDelay);
            await Task.Delay(retryDelay);
        }
    }

    var adminUser = db.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Email == "admin@sistema.com");
    if (adminUser is not null)
    {
        var defaultPassword = "admin123";
        if (!BCrypt.Net.BCrypt.Verify(defaultPassword, adminUser.PasswordHash))
        {
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
            db.SaveChanges();
            logger.LogInformation("Contraseña del administrador restablecida a la predeterminada.");
        }
    }
}

app.Run();
