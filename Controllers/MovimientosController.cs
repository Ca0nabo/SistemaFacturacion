using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Movimientos;
using SistemaFacturacion.Models;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovimientosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public MovimientosController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMovimientoRequest request)
    {
        if (request.Tipo != "CxC" && request.Tipo != "CxP")
            return BadRequest(new { mensaje = "El tipo debe ser 'CxC' o 'CxP'." });

        var factura = await _context.FacturasCabecera.FindAsync(request.IdFactura);
        if (factura is null)
            return BadRequest(new { mensaje = "La factura no existe." });

        var movimiento = new MovimientosCx
        {
            IdFactura = request.IdFactura,
            IdPropiedad = request.IdPropiedad,
            IdUnidad = request.IdUnidad,
            Tipo = request.Tipo,
            MontoOriginal = request.MontoOriginal,
            MontoPendiente = request.MontoPendiente,
            FechaVencimiento = request.FechaVencimiento
        };

        _context.MovimientosCx.Add(movimiento);
        await _context.SaveChangesAsync();

        return Ok(new MovimientoResponse
        {
            IdMovimiento = movimiento.IdMovimiento,
            IdFactura = movimiento.IdFactura,
            IdPropiedad = movimiento.IdPropiedad,
            IdUnidad = movimiento.IdUnidad,
            Tipo = movimiento.Tipo,
            MontoOriginal = movimiento.MontoOriginal,
            MontoPendiente = movimiento.MontoPendiente,
            FechaVencimiento = movimiento.FechaVencimiento,
            EstadoFactura = factura.Estado
        });
    }

    [HttpPost("gasto")]
    public async Task<IActionResult> CreateGasto([FromForm] CreateGastoRequest request, IFormFile? archivo)
    {
        if (request.Tipo != "CxP")
            return BadRequest(new { mensaje = "El tipo debe ser 'CxP' para gastos." });

        var factura = await _context.FacturasCabecera.FindAsync(request.IdFactura);
        if (factura is null)
            return BadRequest(new { mensaje = "La factura no existe." });

        string? archivoEvidencia = null;
        if (archivo is not null && archivo.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}_{archivo.FileName}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }
            archivoEvidencia = fileName;
        }

        var movimiento = new MovimientosCx
        {
            IdFactura = request.IdFactura,
            IdPropiedad = request.IdPropiedad,
            IdUnidad = request.IdUnidad,
            Tipo = request.Tipo,
            MontoOriginal = request.MontoOriginal,
            MontoPendiente = request.MontoOriginal,
            FechaVencimiento = request.FechaVencimiento,
            CategoriaGasto = request.CategoriaGasto,
            ArchivoEvidencia = archivoEvidencia
        };

        _context.MovimientosCx.Add(movimiento);
        await _context.SaveChangesAsync();

        return Ok(new MovimientoResponse
        {
            IdMovimiento = movimiento.IdMovimiento,
            IdFactura = movimiento.IdFactura,
            IdPropiedad = movimiento.IdPropiedad,
            IdUnidad = movimiento.IdUnidad,
            Tipo = movimiento.Tipo,
            MontoOriginal = movimiento.MontoOriginal,
            MontoPendiente = movimiento.MontoPendiente,
            FechaVencimiento = movimiento.FechaVencimiento,
            EstadoFactura = factura.Estado,
            CategoriaGasto = movimiento.CategoriaGasto,
            ArchivoEvidencia = movimiento.ArchivoEvidencia
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? tipo)
    {
        var query = _context.MovimientosCx
            .Include(m => m.Factura)
            .Include(m => m.Propiedad)
            .Include(m => m.Unidad)
            .AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && (tipo == "CxC" || tipo == "CxP"))
            query = query.Where(m => m.Tipo == tipo);

        var movimientos = await query
            .OrderByDescending(m => m.FechaVencimiento)
            .Select(m => new MovimientoResponse
            {
                IdMovimiento = m.IdMovimiento,
                IdFactura = m.IdFactura,
                IdPropiedad = m.IdPropiedad,
                IdUnidad = m.IdUnidad,
                DireccionPropiedad = m.Propiedad != null ? m.Propiedad.Direccion : null,
                CodigoUnidad = m.Unidad != null ? m.Unidad.Codigo : null,
                Tipo = m.Tipo,
                MontoOriginal = m.MontoOriginal,
                MontoPendiente = m.MontoPendiente,
                FechaVencimiento = m.FechaVencimiento,
                EstadoFactura = m.Factura.Estado,
                NumeroCuota = m.NumeroCuota,
                TotalCuotas = m.TotalCuotas,
                CategoriaGasto = m.CategoriaGasto,
                ArchivoEvidencia = m.ArchivoEvidencia
            })
            .ToListAsync();

        return Ok(movimientos);
    }

    [HttpPut("{id}/pagar")]
    public async Task<IActionResult> Pagar(int id)
    {
        var movimiento = await _context.MovimientosCx
            .Include(m => m.Factura)
            .FirstOrDefaultAsync(m => m.IdMovimiento == id);

        if (movimiento is null)
            return NotFound(new { mensaje = "Movimiento no encontrado." });

        movimiento.MontoPendiente = 0;
        movimiento.Factura.Estado = "PAGADA";

        await _context.SaveChangesAsync();

        return Ok(new MovimientoResponse
        {
            IdMovimiento = movimiento.IdMovimiento,
            IdFactura = movimiento.IdFactura,
            IdPropiedad = movimiento.IdPropiedad,
            IdUnidad = movimiento.IdUnidad,
            Tipo = movimiento.Tipo,
            MontoOriginal = movimiento.MontoOriginal,
            MontoPendiente = movimiento.MontoPendiente,
            FechaVencimiento = movimiento.FechaVencimiento,
            EstadoFactura = movimiento.Factura.Estado,
            NumeroCuota = movimiento.NumeroCuota,
            TotalCuotas = movimiento.TotalCuotas,
            CategoriaGasto = movimiento.CategoriaGasto,
            ArchivoEvidencia = movimiento.ArchivoEvidencia
        });
    }

    [HttpGet("vencidas")]
    public async Task<IActionResult> GetVencidas()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var vencidas = await _context.MovimientosCx
            .Include(m => m.Factura)
            .Include(m => m.Propiedad)
            .Include(m => m.Unidad)
            .Where(m => m.FechaVencimiento < hoy && m.MontoPendiente > 0)
            .OrderBy(m => m.FechaVencimiento)
            .Select(m => new MovimientoResponse
            {
                IdMovimiento = m.IdMovimiento,
                IdFactura = m.IdFactura,
                IdPropiedad = m.IdPropiedad,
                IdUnidad = m.IdUnidad,
                DireccionPropiedad = m.Propiedad != null ? m.Propiedad.Direccion : null,
                CodigoUnidad = m.Unidad != null ? m.Unidad.Codigo : null,
                Tipo = m.Tipo,
                MontoOriginal = m.MontoOriginal,
                MontoPendiente = m.MontoPendiente,
                FechaVencimiento = m.FechaVencimiento,
                EstadoFactura = m.Factura.Estado,
                NumeroCuota = m.NumeroCuota,
                TotalCuotas = m.TotalCuotas,
                CategoriaGasto = m.CategoriaGasto,
                ArchivoEvidencia = m.ArchivoEvidencia
            })
            .ToListAsync();

        return Ok(vencidas);
    }
}
