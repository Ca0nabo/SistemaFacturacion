using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Entidades;
using SistemaFacturacion.Models;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntidadesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EntidadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entidades = await _context.Entidades
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entidad = await _context.Entidades.FindAsync(id);
        if (entidad is null)
            return NotFound(new { mensaje = "Entidad no encontrada." });

        return Ok(new EntidadResponse
        {
            IdEntidad = entidad.IdEntidad,
            Tipo = entidad.Tipo,
            RncCedula = entidad.RncCedula,
            RazonSocial = entidad.RazonSocial,
            Activo = entidad.Activo
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEntidadRequest request)
    {
        if (await _context.Entidades.AnyAsync(e => e.RncCedula == request.RncCedula))
            return Conflict(new { mensaje = "Ya existe una entidad con ese RNC/Cédula." });

        var entidad = new Entidad
        {
            Tipo = request.Tipo,
            RncCedula = request.RncCedula,
            RazonSocial = request.RazonSocial,
            Activo = true
        };

        _context.Entidades.Add(entidad);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entidad.IdEntidad }, new EntidadResponse
        {
            IdEntidad = entidad.IdEntidad,
            Tipo = entidad.Tipo,
            RncCedula = entidad.RncCedula,
            RazonSocial = entidad.RazonSocial,
            Activo = entidad.Activo
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateEntidadRequest request)
    {
        var entidad = await _context.Entidades.FindAsync(id);
        if (entidad is null)
            return NotFound(new { mensaje = "Entidad no encontrada." });

        if (await _context.Entidades.AnyAsync(e => e.RncCedula == request.RncCedula && e.IdEntidad != id))
            return Conflict(new { mensaje = "Ya existe otra entidad con ese RNC/Cédula." });

        entidad.Tipo = request.Tipo;
        entidad.RncCedula = request.RncCedula;
        entidad.RazonSocial = request.RazonSocial;

        await _context.SaveChangesAsync();

        return Ok(new EntidadResponse
        {
            IdEntidad = entidad.IdEntidad,
            Tipo = entidad.Tipo,
            RncCedula = entidad.RncCedula,
            RazonSocial = entidad.RazonSocial,
            Activo = entidad.Activo
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var entidad = await _context.Entidades.FindAsync(id);
        if (entidad is null)
            return NotFound(new { mensaje = "Entidad no encontrada." });

        entidad.Activo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
