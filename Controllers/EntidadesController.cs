using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Entidades;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;
using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntidadesController : ControllerBase
{
    private static readonly string[] TiposValidos = ["Cliente", "Propietario", "Proveedor"];
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public EntidadesController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EntidadesVer)]
    public async Task<IActionResult> GetAll([FromQuery] string? tipo, [FromQuery] bool incluirInactivos = false)
    {
        var query = _context.Entidades.AsQueryable();
        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(e => e.Tipo == tipo);
        if (!incluirInactivos)
            query = query.Where(e => e.Activo);

        var entidades = await query
            .OrderBy(e => e.RazonSocial)
            .Select(e => new EntidadResponse
            {
                IdEntidad = e.IdEntidad,
                Tipo = e.Tipo,
                RncCedula = e.RncCedula,
                RazonSocial = e.RazonSocial,
                Activo = e.Activo
            })
            .ToListAsync();

        return Ok(entidades);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.EntidadesVer)]
    public async Task<IActionResult> GetById(int id)
    {
        var entidad = await _context.Entidades.FindAsync(id);
        if (entidad is null)
            return NotFound(new { mensaje = "Entidad no encontrada." });

        return Ok(ToResponse(entidad));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EntidadesGestionar)]
    public async Task<IActionResult> Create([FromBody] CreateEntidadRequest request)
    {
        var tipo = NormalizarTipo(request.Tipo);
        if (tipo is null)
            return BadRequest(new { mensaje = $"Tipo inválido. Valores permitidos: {string.Join(", ", TiposValidos)}." });

        var resultadoDocumento = NormalizarDocumento(request.RncCedula, tipo);
        if (resultadoDocumento.Error is not null)
            return BadRequest(new { mensaje = resultadoDocumento.Error });

        var documento = resultadoDocumento.Documento!;
        var documentoSinGuiones = SoloDigitos(documento);
        if (await _context.Entidades.AnyAsync(e => e.RncCedula == documento || e.RncCedula == documentoSinGuiones))
            return Conflict(new { mensaje = "Ya existe una entidad con ese RNC/Cédula." });

        var entidad = new Entidad
        {
            Tipo = tipo,
            RncCedula = documento,
            RazonSocial = request.RazonSocial.Trim(),
            Activo = true
        };

        _context.Entidades.Add(entidad);
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Entidades", entidad.IdEntidad, $"{tipo}: {entidad.RazonSocial}");

        return CreatedAtAction(nameof(GetById), new { id = entidad.IdEntidad }, ToResponse(entidad));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.EntidadesGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateEntidadRequest request)
    {
        var entidad = await _context.Entidades.FindAsync(id);
        if (entidad is null)
            return NotFound(new { mensaje = "Entidad no encontrada." });

        var tipo = NormalizarTipo(request.Tipo);
        if (tipo is null)
            return BadRequest(new { mensaje = $"Tipo inválido. Valores permitidos: {string.Join(", ", TiposValidos)}." });

        var resultadoDocumento = NormalizarDocumento(request.RncCedula, tipo);
        if (resultadoDocumento.Error is not null)
            return BadRequest(new { mensaje = resultadoDocumento.Error });

        var documento = resultadoDocumento.Documento!;
        var documentoSinGuiones = SoloDigitos(documento);
        if (await _context.Entidades.AnyAsync(e => (e.RncCedula == documento || e.RncCedula == documentoSinGuiones) && e.IdEntidad != id))
            return Conflict(new { mensaje = "Ya existe otra entidad con ese RNC/Cédula." });

        entidad.Tipo = tipo;
        entidad.RncCedula = documento;
        entidad.RazonSocial = request.RazonSocial.Trim();
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Entidades", entidad.IdEntidad);

        return Ok(ToResponse(entidad));
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = Permissions.EntidadesGestionar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
    {
        var entidad = await _context.Entidades.FindAsync(id);
        if (entidad is null)
            return NotFound(new { mensaje = "Entidad no encontrada." });

        entidad.Activo = activo;
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Entidades", entidad.IdEntidad, activo ? "Activada" : "Desactivada");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.EntidadesGestionar)]
    public async Task<IActionResult> Deactivate(int id) => await CambiarEstado(id, false);

    private static EntidadResponse ToResponse(Entidad entidad) => new()
    {
        IdEntidad = entidad.IdEntidad,
        Tipo = entidad.Tipo,
        RncCedula = entidad.RncCedula,
        RazonSocial = entidad.RazonSocial,
        Activo = entidad.Activo
    };

    private static string? NormalizarTipo(string tipo)
    {
        return TiposValidos.FirstOrDefault(t => t.Equals(tipo?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static (string? Documento, string? Error) NormalizarDocumento(string valor, string tipo)
    {
        var digitos = SoloDigitos(valor);

        if (tipo == "Cliente" && digitos.Length != 11)
            return (null, "La cédula del inquilino debe tener exactamente 11 números.");

        if (tipo != "Cliente" && digitos.Length != 9 && digitos.Length != 11)
            return (null, "Introduzca una cédula de 11 números o un RNC de 9 números.");

        return digitos.Length switch
        {
            11 => ($"{digitos[..3]}-{digitos.Substring(3, 7)}-{digitos[10]}", null),
            9 => ($"{digitos[..3]}-{digitos.Substring(3, 5)}-{digitos[8]}", null),
            _ => (null, "Documento inválido.")
        };
    }

    private static string SoloDigitos(string? valor)
    {
        return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
