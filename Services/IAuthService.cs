using SistemaFacturacion.DTOs.Auth;
using SistemaFacturacion.Models;

namespace SistemaFacturacion.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<AuthResponse> AutoLoginAsync(string email);

    Task CambiarPasswordAsync(
        int idUsuario,
        string passwordActual,
        string nuevaPassword
    );

    Task<List<User>> GetAllUsersAsync();

    Task<User?> GetUserByIdAsync(int id);

    Task ToggleUserStatusAsync(int id);

    Task UpdateUserRoleAsync(int id, int idRol);

    Task<AuthResponse> UpdateUserProfileAsync(
        int idUsuario,
        UpdateUserRequest request
    );
}