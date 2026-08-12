using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Movimientos;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovimientosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IAuditLogService _auditLog;

    public MovimientosController(ApplicationDbContext context, IWebHostEnvironment env, IAuditLogService auditLog)
    {
        _context = context;
        _env = env;
        _auditLog = auditLog;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.MovimientosVer)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tipo,
        [FromQuery] int? idEntidad,
        [FromQuery] int? idPropiedad,
        [FromQuery] int? idPropietario,
        [FromQuery] int? idContrato,
        [FromQuery] bool soloPendientes = false)
    {
        var query = _context.MovimientosCx
            .Include(m => m.Factura).ThenInclude(f => f.Entidad)
            .Include(m => m.Factura).ThenInclude(f => f.Contrato)
            .Include(m => m.Propiedad)
            .Include(m => m.Unidad)
            .AsQueryable();

        if (tipo is "CxC" or "CxP") query = query.Where(m => m.Tipo == tipo);
        if (idEntidad.HasValue) query = query.Where(m => m.Factura.IdEntidad == idEntidad.Value);
        if (idPropiedad.HasValue) query = query.Where(m => m.IdPropiedad == idPropiedad.Value);
        if (idPropietario.HasValue) query = query.Where(m => m.Propiedad != null && m.Propiedad.IdEntidad == idPropietario.Value);
        if (idContrato.HasValue) query = query.Where(m => m.Factura.IdContrato == idContrato.Value);
        if (soloPendientes) query = query.Where(m => m.MontoPendiente > 0);

        var movimientos = await query.OrderBy(m => m.FechaVencimiento).ToListAsync();
        return Ok(movimientos.Select(m => MapCxResponse(m)));
    }

    [HttpGet("cuenta")]
    [Authorize(Policy = Permissions.MovimientosVer)]
    public async Task<IActionResult> GetCuenta(
        [FromQuery] int? idEntidad,
        [FromQuery] int? idPropiedad,
        [FromQuery] int? idPropietario,
        [FromQuery] int? idContrato,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var query = _context.MovimientosCuenta
            .Include(m => m.Entidad)
            .Include(m => m.Propiedad)
            .Include(m => m.Contrato)
            .Include(m => m.Factura)
            .AsQueryable();

        if (idEntidad.HasValue) query = query.Where(m => m.IdEntidad == idEntidad.Value);
        if (idPropiedad.HasValue) query = query.Where(m => m.IdPropiedad == idPropiedad.Value);
        if (idPropietario.HasValue) query = query.Where(m => m.Propiedad != null && m.Propiedad.IdEntidad == idPropietario.Value);
        if (idContrato.HasValue) query = query.Where(m => m.IdContrato == idContrato.Value);

        decimal saldo = 0;
        if (desde.HasValue)
        {
            var desdeUtc = desde.Value.ToUniversalTime();
            saldo = await query
                .Where(m => m.Fecha < desdeUtc)
                .SumAsync(m => m.Debito - m.Credito);
            query = query.Where(m => m.Fecha >= desdeUtc);
        }
        if (hasta.HasValue)
            query = query.Where(m => m.Fecha < hasta.Value.Date.AddDays(1).ToUniversalTime());

        var datos = await query
            .OrderBy(m => m.Fecha)
            .ThenBy(m => m.IdMovimientoCuenta)
            .ToListAsync();

        var respuesta = datos.Select(m =>
        {
            saldo += m.Debito - m.Credito;
            return new MovimientoCuentaResponse
            {
                IdMovimientoCuenta = m.IdMovimientoCuenta,
                Fecha = m.Fecha,
                IdEntidad = m.IdEntidad,
                Entidad = m.Entidad.RazonSocial,
                IdPropiedad = m.IdPropiedad,
                Propiedad = m.Propiedad is null ? null : $"{m.Propiedad.Codigo} - {m.Propiedad.Direccion}",
                IdContrato = m.IdContrato,
                CodigoContrato = m.IdContrato.HasValue ? $"CTR-{m.IdContrato.Value:D6}" : null,
                IdFactura = m.IdFactura,
                NumeroFactura = m.Factura?.NumeroECF,
                TipoMovimiento = m.TipoMovimiento,
                Concepto = m.Concepto,
                Referencia = m.Referencia,
                Debito = m.Debito,
                Credito = m.Credito,
                Saldo = saldo
            };
        }).ToList();

        return Ok(respuesta);
    }

    [HttpGet("resumen")]
    [Authorize(Policy = Permissions.MovimientosVer)]
    public async Task<IActionResult> GetResumen(
        [FromQuery] int? idEntidad,
        [FromQuery] int? idPropiedad,
        [FromQuery] int? idPropietario,
        [FromQuery] int? idContrato,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var query = _context.MovimientosCuenta.Include(m => m.Propiedad).AsQueryable();
        if (idEntidad.HasValue) query = query.Where(m => m.IdEntidad == idEntidad.Value);
        if (idPropiedad.HasValue) query = query.Where(m => m.IdPropiedad == idPropiedad.Value);
        if (idPropietario.HasValue) query = query.Where(m => m.Propiedad != null && m.Propiedad.IdEntidad == idPropietario.Value);
        if (idContrato.HasValue) query = query.Where(m => m.IdContrato == idContrato.Value);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value.ToUniversalTime());
        if (hasta.HasValue) query = query.Where(m => m.Fecha < hasta.Value.Date.AddDays(1).ToUniversalTime());

        var debitos = await query.SumAsync(m => m.Debito);
        var creditos = await query.SumAsync(m => m.Credito);
        return Ok(new
        {
            montoFacturado = debitos,
            montoPagado = creditos,
            montoPendiente = Math.Max(0, debitos - creditos)
        });
    }

    [HttpPost("gasto")]
    [RequestSizeLimit(5_000_000)]
    [Authorize(Policy = Permissions.GastosGestionar)]
    public async Task<IActionResult> CreateGasto([FromForm] CreateGastoRequest request, IFormFile? archivo)
    {
        if (request.Tipo != "CxP")
            return BadRequest(new { mensaje = "El tipo debe ser CxP para registrar gastos." });

        var factura = await _context.FacturasCabecera
            .Include(f => f.Entidad)
            .FirstOrDefaultAsync(f => f.IdFactura == request.IdFactura);
        if (factura is null)
            return BadRequest(new { mensaje = "La factura no existe." });

        string? archivoEvidencia = null;
        if (archivo is not null && archivo.Length > 0)
        {
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var permitidas = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            if (!permitidas.Contains(ext))
                return BadRequest(new { mensaje = "Solo se permiten archivos PDF, PNG o JPG." });

            var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(uploadsDir, fileName));
            await archivo.CopyToAsync(stream);
            archivoEvidencia = fileName;
        }

        var movimiento = new MovimientosCx
        {
            IdFactura = request.IdFactura,
            IdPropiedad = request.IdPropiedad,
            IdUnidad = request.IdUnidad,
            Tipo = "CxP",
            MontoOriginal = request.MontoOriginal,
            MontoPendiente = request.MontoOriginal,
            FechaVencimiento = request.FechaVencimiento,
            CategoriaGasto = request.CategoriaGasto.Trim(),
            ArchivoEvidencia = archivoEvidencia
        };

        _context.MovimientosCx.Add(movimiento);
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Gastos", movimiento.IdMovimiento, movimiento.CategoriaGasto);
        return Ok(MapCxResponse(movimiento, factura));
    }

    [HttpPut("{id:int}/pagar")]
    [Authorize(Policy = Permissions.FacturasPagar)]
    public async Task<IActionResult> PagarCompleto(int id)
    {
        var movimiento = await _context.MovimientosCx
            .Include(m => m.Factura).ThenInclude(f => f.Pagos)
            .Include(m => m.Factura).ThenInclude(f => f.Movimientos)
            .FirstOrDefaultAsync(m => m.IdMovimiento == id);
        if (movimiento is null)
            return NotFound(new { mensaje = "Movimiento no encontrado." });
        if (movimiento.MontoPendiente <= 0)
            return BadRequest(new { mensaje = "El movimiento ya está saldado." });

        if (movimiento.Tipo == "CxC")
        {
            var factura = movimiento.Factura;
            if (factura.Estado == "ANULADA")
                return BadRequest(new { mensaje = "No se puede pagar una factura anulada." });
            if (await _context.AcuerdosPago.AnyAsync(a => a.IdFacturaOrigen == factura.IdFactura && a.Estado == "Activo"))
                return BadRequest(new { mensaje = "La factura tiene un acuerdo de pago activo. Registre el pago desde sus cuotas." });

            var monto = movimiento.MontoPendiente;
            var pagadoAntes = factura.Pagos.Sum(p => p.Monto);
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pago = new Pago
                {
                    IdFactura = factura.IdFactura,
                    IdContrato = factura.IdContrato,
                    IdEntidad = factura.IdEntidad,
                    IdPropiedad = factura.IdPropiedad,
                    IdUnidad = factura.IdUnidad,
                    Monto = monto,
                    FechaPago = DateTime.UtcNow,
                    MetodoPago = "Transferencia",
                    Referencia = "Pago completo registrado desde movimientos"
                };
                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                var porAplicar = monto;
                foreach (var cx in factura.Movimientos.Where(m => m.MontoPendiente > 0).OrderBy(m => m.FechaVencimiento))
                {
                    var aplicado = Math.Min(cx.MontoPendiente, porAplicar);
                    cx.MontoPendiente -= aplicado;
                    porAplicar -= aplicado;
                    if (porAplicar <= 0) break;
                }

                var totalPagado = pagadoAntes + monto;
                factura.Estado = totalPagado >= factura.Total ? "PAGADA" : "PARCIAL";

                _context.MovimientosCuenta.Add(new MovimientoCuenta
                {
                    IdEntidad = factura.IdEntidad,
                    IdPropiedad = factura.IdPropiedad,
                    IdUnidad = factura.IdUnidad,
                    IdContrato = factura.IdContrato,
                    IdFactura = factura.IdFactura,
                    IdPago = pago.IdPago,
                    Fecha = pago.FechaPago,
                    TipoMovimiento = "Pago",
                    Concepto = $"Pago aplicado a {factura.NumeroECF}",
                    Referencia = pago.Referencia,
                    Debito = 0,
                    Credito = pago.Monto
                });

                _context.AsientosContables.AddRange(
                    new AsientoContable
                    {
                        IdFacturaReferencia = factura.IdFactura,
                        IdCuentaContable = 2,
                        MontoDebito = pago.Monto,
                        MontoCredito = 0,
                        Descripcion = $"Cobro factura {factura.NumeroECF}"
                    },
                    new AsientoContable
                    {
                        IdFacturaReferencia = factura.IdFactura,
                        IdCuentaContable = 3,
                        MontoDebito = 0,
                        MontoCredito = pago.Monto,
                        Descripcion = $"Disminución CxC factura {factura.NumeroECF}"
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _auditLog.LogFromContextAsync(HttpContext, "PAGAR", "Facturacion", factura.IdFactura, $"RD${monto:N2}");
                return Ok(new { mensaje = "Pago completo registrado.", factura.IdFactura, factura.Estado });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        movimiento.MontoPendiente = 0;
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "PAGAR", "Gastos", movimiento.IdMovimiento);
        return Ok(MapCxResponse(movimiento));
    }

    [HttpGet("vencidas")]
    [Authorize(Policy = Permissions.MovimientosVer)]
    public async Task<IActionResult> GetVencidas()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var vencidas = await _context.MovimientosCx
            .Include(m => m.Factura).ThenInclude(f => f.Entidad)
            .Include(m => m.Propiedad)
            .Include(m => m.Unidad)
            .Where(m => m.FechaVencimiento < hoy && m.MontoPendiente > 0)
            .OrderBy(m => m.FechaVencimiento)
            .ToListAsync();

        return Ok(vencidas.Select(m => MapCxResponse(m)));
    }

    private static MovimientoResponse MapCxResponse(MovimientosCx m, FacturaCabecera? facturaCargada = null)
    {
        var factura = facturaCargada ?? m.Factura;
        return new MovimientoResponse
        {
            IdMovimiento = m.IdMovimiento,
            IdFactura = m.IdFactura,
            NumeroFactura = factura?.NumeroECF ?? string.Empty,
            IdEntidad = factura?.IdEntidad ?? 0,
            Entidad = factura?.Entidad?.RazonSocial ?? string.Empty,
            IdContrato = factura?.IdContrato,
            CodigoContrato = factura?.IdContrato is int id ? $"CTR-{id:D6}" : null,
            IdPropiedad = m.IdPropiedad,
            IdUnidad = m.IdUnidad,
            DireccionPropiedad = m.Propiedad?.Direccion,
            CodigoUnidad = m.Unidad?.Codigo,
            Tipo = m.Tipo,
            MontoOriginal = m.MontoOriginal,
            MontoPendiente = m.MontoPendiente,
            FechaVencimiento = m.FechaVencimiento,
            EstadoFactura = factura?.Estado ?? string.Empty,
            NumeroCuota = m.NumeroCuota,
            TotalCuotas = m.TotalCuotas,
            CategoriaGasto = m.CategoriaGasto,
            ArchivoEvidencia = m.ArchivoEvidencia
        };
    }
}
