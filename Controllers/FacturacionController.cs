using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Facturacion;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacturacionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDgiiMockService _dgiiMock;
    private readonly IAuditLogService _auditLog;

    public FacturacionController(ApplicationDbContext context, IDgiiMockService dgiiMock, IAuditLogService auditLog)
    {
        _context = context;
        _dgiiMock = dgiiMock;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<IActionResult> Emitir([FromBody] CreateFacturaRequest request)
    {
        var entidad = await _context.Entidades.FindAsync(request.IdEntidad);
        if (entidad is null)
            return BadRequest(new { mensaje = "La entidad (cliente/proveedor) no existe." });

        if (!entidad.Activo)
            return BadRequest(new { mensaje = "No se puede emitir una factura a una entidad inactiva." });

        if (request.Detalles is null || request.Detalles.Count == 0)
            return BadRequest(new { mensaje = "La factura debe tener al menos un detalle." });

        if (request.IdPropiedad.HasValue)
        {
            var propiedad = await _context.Propiedades.FindAsync(request.IdPropiedad.Value);
            if (propiedad is null)
                return BadRequest(new { mensaje = "La propiedad no existe." });
        }

        var subtotal = request.Detalles.Sum(d => d.Cantidad * d.Precio);

        var parametros = await _context.ParametrosEmpresa.FirstOrDefaultAsync();
        var porcentajeItbis = parametros?.PorcentajeITBIS ?? 0.18m;
        var itbis = subtotal * porcentajeItbis;
        var total = subtotal + itbis;

        var numeroEcf = $"ECF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        var factura = new FacturaCabecera
        {
            IdEntidad = request.IdEntidad,
            IdPropiedad = request.IdPropiedad,
            IdUnidad = request.IdUnidad,
            NumeroECF = numeroEcf,
            FechaEmision = DateTime.UtcNow,
            Subtotal = subtotal,
            Itbis = itbis,
            Total = total,
            Estado = "EMITIDA"
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.FacturasCabecera.Add(factura);
            await _context.SaveChangesAsync();

            foreach (var item in request.Detalles)
            {
                _context.FacturasDetalle.Add(new FacturaDetalle
                {
                    IdFactura = factura.IdFactura,
                    DescripcionItem = item.DescripcionItem,
                    Cantidad = item.Cantidad,
                    Precio = item.Precio
                });
            }

            await _context.SaveChangesAsync();

            var firma = await _dgiiMock.FirmarECFAsync(factura.IdFactura, total);

            factura.FirmaDGII = firma;
            factura.NumeroECF = $"ECF{parametros?.SecuenciaFiscalECF ?? "ECF001"}-{factura.IdFactura:D6}";
            await _context.SaveChangesAsync();

            if (request.Cuotas.HasValue && request.Cuotas.Value > 1)
            {
                var montoCuota = Math.Round(total / request.Cuotas.Value, 2);
                var remainder = total - (montoCuota * request.Cuotas.Value);
                for (int i = 1; i <= request.Cuotas.Value; i++)
                {
                    var monto = i == request.Cuotas.Value ? montoCuota + remainder : montoCuota;
                    _context.MovimientosCx.Add(new MovimientosCx
                    {
                        IdFactura = factura.IdFactura,
                        Tipo = "CxC",
                        MontoOriginal = monto,
                        MontoPendiente = monto,
                        FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(i)),
                        NumeroCuota = i,
                        TotalCuotas = request.Cuotas.Value
                    });
                }
            }
            else
            {
                _context.MovimientosCx.Add(new MovimientosCx
                {
                    IdFactura = factura.IdFactura,
                    Tipo = "CxC",
                    MontoOriginal = total,
                    MontoPendiente = total,
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                    NumeroCuota = 1,
                    TotalCuotas = 1
                });
            }

            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 3,
                MontoDebito = total,
                MontoCredito = 0,
                Descripcion = $"Factura #{factura.NumeroECF} - Cuentas por Cobrar"
            });

            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 5,
                MontoDebito = 0,
                MontoCredito = subtotal,
                Descripcion = $"Factura #{factura.NumeroECF} - Ingresos por Ventas"
            });

            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 6,
                MontoDebito = 0,
                MontoCredito = itbis,
                Descripcion = $"Factura #{factura.NumeroECF} - ITBIS por Pagar"
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Facturacion", factura.IdFactura);

            var response = new FacturaResponse
            {
                IdFactura = factura.IdFactura,
                NumeroECF = factura.NumeroECF,
                RazonSocial = entidad.RazonSocial,
                RncCedula = entidad.RncCedula,
                IdPropiedad = factura.IdPropiedad,
                IdUnidad = factura.IdUnidad,
                FechaEmision = factura.FechaEmision,
                Subtotal = factura.Subtotal,
                Itbis = factura.Itbis,
                Total = factura.Total,
                Estado = factura.Estado,
                FirmaDGII = firma,
                Detalles = request.Detalles.Select(d => new FacturaDetalleResponse
                {
                    DescripcionItem = d.DescripcionItem,
                    Cantidad = d.Cantidad,
                    Precio = d.Precio,
                    Subtotal = d.Cantidad * d.Precio
                }).ToList()
            };

            return CreatedAtAction(nameof(GetById), new { id = factura.IdFactura }, response);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var facturas = await _context.FacturasCabecera
            .Include(f => f.Entidad)
            .Include(f => f.Propiedad)
            .Include(f => f.Unidad)
            .OrderByDescending(f => f.FechaEmision)
            .Select(f => new FacturaResponse
            {
                IdFactura = f.IdFactura,
                NumeroECF = f.NumeroECF,
                RazonSocial = f.Entidad.RazonSocial,
                RncCedula = f.Entidad.RncCedula,
                IdPropiedad = f.IdPropiedad,
                DireccionPropiedad = f.Propiedad != null ? f.Propiedad.Direccion : null,
                IdUnidad = f.IdUnidad,
                CodigoUnidad = f.Unidad != null ? f.Unidad.Codigo : null,
                FechaEmision = f.FechaEmision,
                Subtotal = f.Subtotal,
                Itbis = f.Itbis,
                Total = f.Total,
                Estado = f.Estado,
                FirmaDGII = f.FirmaDGII ?? ""
            })
            .ToListAsync();

        return Ok(facturas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var factura = await _context.FacturasCabecera
            .Include(f => f.Entidad)
            .Include(f => f.Propiedad)
            .Include(f => f.Unidad)
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.IdFactura == id);

        if (factura is null)
            return NotFound(new { mensaje = "Factura no encontrada." });

        var response = new FacturaResponse
        {
            IdFactura = factura.IdFactura,
            NumeroECF = factura.NumeroECF,
            RazonSocial = factura.Entidad.RazonSocial,
            RncCedula = factura.Entidad.RncCedula,
            IdPropiedad = factura.IdPropiedad,
            DireccionPropiedad = factura.Propiedad?.Direccion,
            IdUnidad = factura.IdUnidad,
            CodigoUnidad = factura.Unidad?.Codigo,
            FechaEmision = factura.FechaEmision,
            Subtotal = factura.Subtotal,
            Itbis = factura.Itbis,
            Total = factura.Total,
            Estado = factura.Estado,
            FirmaDGII = factura.FirmaDGII ?? "",
            Detalles = factura.Detalles.Select(d => new FacturaDetalleResponse
            {
                DescripcionItem = d.DescripcionItem,
                Cantidad = d.Cantidad,
                Precio = d.Precio,
                Subtotal = d.Cantidad * d.Precio
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPut("{id}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        var factura = await _context.FacturasCabecera
            .Include(f => f.Movimientos)
            .FirstOrDefaultAsync(f => f.IdFactura == id);

        if (factura is null)
            return NotFound(new { mensaje = "Factura no encontrada." });

        if (factura.Estado == "ANULADA")
            return BadRequest(new { mensaje = "La factura ya está anulada." });

        factura.Estado = "ANULADA";

        foreach (var mov in factura.Movimientos)
        {
            mov.MontoPendiente = 0;
        }

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "ANULAR", "Facturacion", factura.IdFactura);

        return NoContent();
    }
}
