using System.Security.Cryptography;
using System.Text;

namespace SistemaFacturacion.Services;

public class DgiiMockService : IDgiiMockService
{
    public Task<string> FirmarECFAsync(int idFactura, decimal total)
    {
        var input = $"ECF-{idFactura:D6}|{total:F2}|{DateTime.UtcNow:O}|DGII-MOCK-SECRET";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hashStr = Convert.ToHexString(hash).ToLowerInvariant();
        var firma = $"DGII-{idFactura:D6}-{hashStr[..16]}";
        return Task.FromResult(firma);
    }
}
