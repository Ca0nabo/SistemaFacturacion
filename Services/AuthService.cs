using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Auth;
using SistemaFacturacion.Models;
using SistemaFacturacion.Security;

namespace SistemaFacturacion.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException(
                "El email ya está registrado."
            );

        var rol = await _context.Roles.FindAsync(request.IdRol)
            ?? throw new InvalidOperationException(
                "El rol especificado no existe."
            );

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            NombreCompleto = request.NombreCompleto.Trim(),
            IdRol = request.IdRol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(user);

        await _context.SaveChangesAsync();

        return BuildAuthResponse(user, rol);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new UnauthorizedAccessException(
                "Credenciales inválidas."
            );

        if (!user.Activo)
            throw new UnauthorizedAccessException(
                "La cuenta está desactivada."
            );

        if (!BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Credenciales inválidas."
            );
        }

        return BuildAuthResponse(user, user.Rol);
    }

    // =========================================================
    // ACCESO AUTOMÁTICO PARA MODO DEMO
    // =========================================================

    public async Task<AuthResponse> AutoLoginAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException(
                "No se configuró el usuario de acceso automático."
            );
        }

        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        var user = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(
                u => u.Email == normalizedEmail
            );

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Usuario de acceso automático no encontrado."
            );
        }

        if (!user.Activo)
        {
            throw new UnauthorizedAccessException(
                "El usuario de acceso automático está desactivado."
            );
        }

        return BuildAuthResponse(
            user,
            user.Rol
        );
    }

    public async Task CambiarPasswordAsync(
        int idUsuario,
        string passwordActual,
        string nuevaPassword)
    {
        var user = await _context.Usuarios.FindAsync(idUsuario)
            ?? throw new UnauthorizedAccessException(
                "Usuario no encontrado."
            );

        if (!user.Activo)
        {
            throw new UnauthorizedAccessException(
                "La cuenta está desactivada."
            );
        }

        if (!BCrypt.Net.BCrypt.Verify(
                passwordActual,
                user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "La contraseña actual no es correcta."
            );
        }

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(nuevaPassword);

        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .OrderByDescending(u => u.FechaCreacion)
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(
                u => u.IdUsuario == id
            );
    }

    public async Task ToggleUserStatusAsync(int id)
    {
        var user = await _context.Usuarios.FindAsync(id)
            ?? throw new InvalidOperationException(
                "Usuario no encontrado."
            );

        user.Activo = !user.Activo;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserRoleAsync(
        int id,
        int idRol)
    {
        var user = await _context.Usuarios.FindAsync(id)
            ?? throw new InvalidOperationException(
                "Usuario no encontrado."
            );

        if (!await _context.Roles.AnyAsync(
                r => r.IdRol == idRol))
        {
            throw new InvalidOperationException(
                "El rol no existe."
            );
        }

        user.IdRol = idRol;

        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponse> UpdateUserProfileAsync(
        int idUsuario,
        UpdateUserRequest request)
    {
        var user = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(
                u => u.IdUsuario == idUsuario
            )
            ?? throw new InvalidOperationException(
                "Usuario no encontrado."
            );

        if (!user.Activo)
        {
            throw new UnauthorizedAccessException(
                "La cuenta está desactivada."
            );
        }

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(
                u =>
                    u.Email == email &&
                    u.IdUsuario != idUsuario))
        {
            throw new InvalidOperationException(
                "El email ya está en uso por otro usuario."
            );
        }

        user.NombreCompleto =
            request.NombreCompleto.Trim();

        user.Email = email;

        await _context.SaveChangesAsync();

        return BuildAuthResponse(
            user,
            user.Rol
        );
    }

    private AuthResponse BuildAuthResponse(
        User user,
        Role role)
    {
        return new AuthResponse
        {
            IdUsuario = user.IdUsuario,
            Email = user.Email,
            NombreCompleto = user.NombreCompleto,
            Rol = role.Nombre,

            Permisos = Permissions
                .Expand(role.Permisos)
                .ToList(),

            Token = GenerateToken(
                user,
                role
            )
        };
    }

    private string GenerateToken(
        User user,
        Role role)
    {
        var jwtKey =
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key no configurado."
            );

        var jwtIssuer =
            _configuration["Jwt:Issuer"]
            ?? "SistemaFacturacion";

        var jwtAudience =
            _configuration["Jwt:Audience"]
            ?? "SistemaFacturacionApp";

        var expireMinutes =
            double.TryParse(
                _configuration["Jwt:ExpireMinutes"],
                out var minutes)
                ? minutes
                : 120;

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

        var creds =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.IdUsuario.ToString()
            ),

            new(
                ClaimTypes.Email,
                user.Email
            ),

            new(
                ClaimTypes.Name,
                user.NombreCompleto
            ),

            new(
                ClaimTypes.Role,
                role.Nombre
            )
        };

        foreach (
            var permission
            in Permissions.Expand(role.Permisos))
        {
            claims.Add(
                new Claim(
                    "permission",
                    permission
                )
            );
        }

        var token =
            new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow
                    .AddMinutes(expireMinutes),
                signingCredentials: creds
            );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}