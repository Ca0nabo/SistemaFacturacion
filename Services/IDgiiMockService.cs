namespace SistemaFacturacion.Services;
public interface IDgiiMockService
{
    Task<string> FirmarECFAsync(int idFactura, decimal total);
}
