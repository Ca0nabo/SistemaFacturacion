using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Contratos;
using SistemaFacturacion.Services;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContratosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public ContratosController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var contratos = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .OrderByDescending(c => c.FechaVencimiento)
            .Select(c => new ContratoResponse
            {
                IdContrato = c.IdContrato,
                IdEntidad = c.IdEntidad,
                RazonSocial = c.Entidad.RazonSocial,
                RncCedula = c.Entidad.RncCedula,
                IdPropiedad = c.IdPropiedad,
                DireccionPropiedad = c.Propiedad != null ? c.Propiedad.Direccion : null,
                IdUnidad = c.IdUnidad,
                CodigoUnidad = c.Unidad != null ? c.Unidad.Codigo : null,
                TipoContrato = c.TipoContrato,
                Condiciones = c.Condiciones,
                FechaInicio = c.FechaInicio,
                FechaVencimiento = c.FechaVencimiento,
                Monto = c.Monto,
                MontoMantenimiento = c.MontoMantenimiento,
                Deposito = c.Deposito,
                DiaPago = c.DiaPago,
                Estado = c.Estado
            })
            .ToListAsync();

        return Ok(contratos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .FirstOrDefaultAsync(c => c.IdContrato == id);

        if (contrato is null)
            return NotFound(new { mensaje = "Contrato no encontrado." });

        return Ok(new ContratoResponse
        {
            IdContrato = contrato.IdContrato,
            IdEntidad = contrato.IdEntidad,
            RazonSocial = contrato.Entidad.RazonSocial,
            RncCedula = contrato.Entidad.RncCedula,
            IdPropiedad = contrato.IdPropiedad,
            DireccionPropiedad = contrato.Propiedad?.Direccion,
            IdUnidad = contrato.IdUnidad,
            CodigoUnidad = contrato.Unidad?.Codigo,
            TipoContrato = contrato.TipoContrato,
            Condiciones = contrato.Condiciones,
            FechaInicio = contrato.FechaInicio,
            FechaVencimiento = contrato.FechaVencimiento,
            Monto = contrato.Monto,
            MontoMantenimiento = contrato.MontoMantenimiento,
            Deposito = contrato.Deposito,
            DiaPago = contrato.DiaPago,
            Estado = contrato.Estado
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContratoRequest request)
    {
        var entidad = await _context.Entidades.FindAsync(request.IdEntidad);
        if (entidad is null)
            return BadRequest(new { mensaje = "La entidad no existe." });

        if (request.IdPropiedad.HasValue)
        {
            var propiedad = await _context.Propiedades.FindAsync(request.IdPropiedad.Value);
            if (propiedad is null)
                return BadRequest(new { mensaje = "La propiedad no existe." });

            if (request.IdUnidad.HasValue)
            {
                var unidad = await _context.Unidades.FindAsync(request.IdUnidad.Value);
                if (unidad is null || unidad.IdPropiedad != request.IdPropiedad.Value)
                    return BadRequest(new { mensaje = "La unidad no existe o no pertenece a la propiedad seleccionada." });
            }
        }

        if (request.FechaVencimiento <= request.FechaInicio)
            return BadRequest(new { mensaje = "La fecha de vencimiento debe ser posterior a la fecha de inicio." });

        var contrato = new Models.Contrato
        {
            IdEntidad = request.IdEntidad,
            IdPropiedad = request.IdPropiedad,
            IdUnidad = request.IdUnidad,
            TipoContrato = request.TipoContrato,
            Condiciones = request.Condiciones,
            FechaInicio = request.FechaInicio,
            FechaVencimiento = request.FechaVencimiento,
            Monto = request.Monto,
            MontoMantenimiento = request.MontoMantenimiento,
            Deposito = request.Deposito,
            DiaPago = request.DiaPago,
            Estado = "Pendiente"
        };

        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Contratos", contrato.IdContrato);

        var createResponse = new ContratoResponse
        {
            IdContrato = contrato.IdContrato,
            IdEntidad = contrato.IdEntidad,
            RazonSocial = entidad.RazonSocial,
            RncCedula = entidad.RncCedula,
            IdPropiedad = contrato.IdPropiedad,
            IdUnidad = contrato.IdUnidad,
            TipoContrato = contrato.TipoContrato,
            Condiciones = contrato.Condiciones,
            FechaInicio = contrato.FechaInicio,
            FechaVencimiento = contrato.FechaVencimiento,
            Monto = contrato.Monto,
            MontoMantenimiento = contrato.MontoMantenimiento,
            Deposito = contrato.Deposito,
            DiaPago = contrato.DiaPago,
            Estado = contrato.Estado
        };

        if (request.IdPropiedad.HasValue)
        {
            var prop = await _context.Propiedades.FindAsync(request.IdPropiedad.Value);
            if (prop is not null)
                createResponse.DireccionPropiedad = prop.Direccion;
        }

        return CreatedAtAction(nameof(GetById), new { id = contrato.IdContrato }, createResponse);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateContratoRequest request)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .FirstOrDefaultAsync(c => c.IdContrato == id);
        if (contrato is null)
            return NotFound(new { mensaje = "Contrato no encontrado." });

        if (request.FechaVencimiento <= request.FechaInicio)
            return BadRequest(new { mensaje = "La fecha de vencimiento debe ser posterior a la fecha de inicio." });

        contrato.IdEntidad = request.IdEntidad;
        contrato.IdPropiedad = request.IdPropiedad;
        contrato.IdUnidad = request.IdUnidad;
        contrato.TipoContrato = request.TipoContrato;
        contrato.Condiciones = request.Condiciones;
        contrato.FechaInicio = request.FechaInicio;
        contrato.FechaVencimiento = request.FechaVencimiento;
        contrato.Monto = request.Monto;
        contrato.MontoMantenimiento = request.MontoMantenimiento;
        contrato.Deposito = request.Deposito;
        contrato.DiaPago = request.DiaPago;

        await _context.SaveChangesAsync();

        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Contratos", contrato.IdContrato);

        return Ok(new ContratoResponse
        {
            IdContrato = contrato.IdContrato,
            IdEntidad = contrato.IdEntidad,
            RazonSocial = contrato.Entidad.RazonSocial,
            RncCedula = contrato.Entidad.RncCedula,
            IdPropiedad = contrato.IdPropiedad,
            DireccionPropiedad = contrato.Propiedad?.Direccion,
            IdUnidad = contrato.IdUnidad,
            CodigoUnidad = contrato.Unidad?.Codigo,
            TipoContrato = contrato.TipoContrato,
            Condiciones = contrato.Condiciones,
            FechaInicio = contrato.FechaInicio,
            FechaVencimiento = contrato.FechaVencimiento,
            Monto = contrato.Monto,
            MontoMantenimiento = contrato.MontoMantenimiento,
            Deposito = contrato.Deposito,
            DiaPago = contrato.DiaPago,
            Estado = contrato.Estado
        });
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
    {
        var validos = new[] { "Pendiente", "Activo", "Vencido", "Cancelado" };
        if (!validos.Contains(request.NuevoEstado))
            return BadRequest(new { mensaje = $"Estado inválido. Valores válidos: {string.Join(", ", validos)}" });

        var contrato = await _context.Contratos.FindAsync(id);
        if (contrato is null)
            return NotFound(new { mensaje = "Contrato no encontrado." });

        contrato.Estado = request.NuevoEstado;
        await _context.SaveChangesAsync();

        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Contratos", contrato.IdContrato,
            $"Estado cambiado a: {request.NuevoEstado}");

        return NoContent();
    }
}
