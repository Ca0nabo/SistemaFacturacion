using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Depositos;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepositosController : ControllerBase
{
    private static readonly string[] EstadosValidos = ["Pendiente", "Parcial", "Recibido", "Devuelto", "Aplicado"];
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public DepositosController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.DepositosVer)]
    public async Task<IActionResult> GetAll([FromQuery] int? idContrato, [FromQuery] string? estado)
    {
        var query = _context.DepositosGarantia
            .Include(d => d.Contrato).ThenInclude(c => c.Entidad)
            .Include(d => d.Contrato).ThenInclude(c => c.Propiedad)
            .Where(d => d.Activo)
            .AsQueryable();

        if (idContrato.HasValue) query = query.Where(d => d.IdContrato == idContrato.Value);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(d => d.Estado == estado);

        var datos = await query.OrderByDescending(d => d.IdDeposito).ToListAsync();
        return Ok(datos.Select(MapResponse));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.DepositosVer)]
    public async Task<IActionResult> GetById(int id)
    {
        var deposito = await CargarAsync(id);
        return deposito is null
            ? NotFound(new { mensaje = "Depósito no encontrado." })
            : Ok(MapResponse(deposito));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.DepositosGestionar)]
    public async Task<IActionResult> Create([FromBody] CreateDepositoRequest request)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .FirstOrDefaultAsync(c => c.IdContrato == request.IdContrato);
        if (contrato is null)
            return BadRequest(new { mensaje = "El contrato no existe." });
        if (await _context.DepositosGarantia.AnyAsync(d => d.IdContrato == request.IdContrato && d.Activo))
            return Conflict(new { mensaje = "El contrato ya tiene un depósito activo. Edite el registro existente." });

        var estado = NormalizarEstado(request.Estado, request.MontoRequerido, request.MontoRecibido);
        if (estado is null)
            return BadRequest(new { mensaje = $"Estado inválido. Valores: {string.Join(", ", EstadosValidos)}." });
        if (request.MontoRecibido > request.MontoRequerido)
            return BadRequest(new { mensaje = "El monto recibido no puede superar el depósito requerido." });

        var deposito = new DepositoGarantia
        {
            IdContrato = request.IdContrato,
            MontoRequerido = request.MontoRequerido,
            MontoRecibido = request.MontoRecibido,
            FechaRecepcion = request.MontoRecibido > 0 ? request.FechaRecepcion ?? DateOnly.FromDateTime(DateTime.UtcNow) : null,
            FechaDevolucion = request.FechaDevolucion,
            Estado = estado,
            MetodoPago = request.MetodoPago?.Trim(),
            Referencia = request.Referencia?.Trim(),
            Observaciones = request.Observaciones?.Trim(),
            Activo = true
        };

        _context.DepositosGarantia.Add(deposito);
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Depositos", deposito.IdDeposito, $"Contrato CTR-{contrato.IdContrato:D6}");

        deposito.Contrato = contrato;
        return CreatedAtAction(nameof(GetById), new { id = deposito.IdDeposito }, MapResponse(deposito));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.DepositosGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateDepositoRequest request)
    {
        var deposito = await CargarAsync(id);
        if (deposito is null)
            return NotFound(new { mensaje = "Depósito no encontrado." });
        if (request.IdContrato != deposito.IdContrato)
            return BadRequest(new { mensaje = "No se puede cambiar el contrato de un depósito existente." });
        if (request.MontoRecibido > request.MontoRequerido)
            return BadRequest(new { mensaje = "El monto recibido no puede superar el depósito requerido." });

        var estado = NormalizarEstado(request.Estado, request.MontoRequerido, request.MontoRecibido);
        if (estado is null)
            return BadRequest(new { mensaje = "Estado de depósito inválido." });

        deposito.MontoRequerido = request.MontoRequerido;
        deposito.MontoRecibido = request.MontoRecibido;
        deposito.FechaRecepcion = request.MontoRecibido > 0 ? request.FechaRecepcion ?? deposito.FechaRecepcion ?? DateOnly.FromDateTime(DateTime.UtcNow) : null;
        deposito.FechaDevolucion = request.FechaDevolucion;
        deposito.Estado = estado;
        deposito.MetodoPago = request.MetodoPago?.Trim();
        deposito.Referencia = request.Referencia?.Trim();
        deposito.Observaciones = request.Observaciones?.Trim();

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Depositos", deposito.IdDeposito);
        return Ok(MapResponse(deposito));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.DepositosGestionar)]
    public async Task<IActionResult> Delete(int id)
    {
        var deposito = await _context.DepositosGarantia.FindAsync(id);
        if (deposito is null)
            return NotFound(new { mensaje = "Depósito no encontrado." });

        deposito.Activo = false;
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "ELIMINAR", "Depositos", deposito.IdDeposito);
        return NoContent();
    }

    private async Task<DepositoGarantia?> CargarAsync(int id) => await _context.DepositosGarantia
        .Include(d => d.Contrato).ThenInclude(c => c.Entidad)
        .Include(d => d.Contrato).ThenInclude(c => c.Propiedad)
        .FirstOrDefaultAsync(d => d.IdDeposito == id && d.Activo);

    private static string? NormalizarEstado(string? solicitado, decimal requerido, decimal recibido)
    {
        if (solicitado is "Devuelto" or "Aplicado") return solicitado;
        if (recibido <= 0) return "Pendiente";
        if (recibido < requerido) return "Parcial";
        if (recibido == requerido) return "Recibido";
        return EstadosValidos.FirstOrDefault(e => e.Equals(solicitado, StringComparison.OrdinalIgnoreCase));
    }

    private static DepositoResponse MapResponse(DepositoGarantia d) => new()
    {
        IdDeposito = d.IdDeposito,
        IdContrato = d.IdContrato,
        CodigoContrato = $"CTR-{d.IdContrato:D6}",
        Inquilino = d.Contrato.Entidad.RazonSocial,
        Propiedad = d.Contrato.Propiedad is null ? "Sin propiedad" : $"{d.Contrato.Propiedad.Codigo} - {d.Contrato.Propiedad.Direccion}",
        MontoRequerido = d.MontoRequerido,
        MontoRecibido = d.MontoRecibido,
        FechaRecepcion = d.FechaRecepcion,
        FechaDevolucion = d.FechaDevolucion,
        Estado = d.Estado,
        MetodoPago = d.MetodoPago,
        Referencia = d.Referencia,
        Observaciones = d.Observaciones,
        Activo = d.Activo
    };
}
