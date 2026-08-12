using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Contratos;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

using SistemaFacturacion.Security;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContratosController : ControllerBase
{
    private static readonly string[] EstadosValidos = ["Pendiente", "Activo", "Vencido", "Cancelado"];
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public ContratosController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.ContratosVer)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] int? idEntidad,
        [FromQuery] int? idPropiedad)
    {
        await ActualizarContratosVencidosAsync();

        var query = _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(c => c.Estado == estado);
        if (idEntidad.HasValue)
            query = query.Where(c => c.IdEntidad == idEntidad.Value);
        if (idPropiedad.HasValue)
            query = query.Where(c => c.IdPropiedad == idPropiedad.Value);

        var contratos = await query
            .OrderByDescending(c => c.FechaInicio)
            .ToListAsync();

        return Ok(contratos.Select(MapResponse));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.ContratosVer)]
    public async Task<IActionResult> GetById(int id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .FirstOrDefaultAsync(c => c.IdContrato == id);

        return contrato is null
            ? NotFound(new { mensaje = "Contrato no encontrado." })
            : Ok(MapResponse(contrato));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ContratosGestionar)]
    public async Task<IActionResult> Create([FromBody] CreateContratoRequest request)
    {
        var validacion = await ValidarRequestAsync(request, null);
        if (validacion is not null)
            return BadRequest(new { mensaje = validacion });

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var estadoInicial = request.FechaInicio > hoy ? "Pendiente" : request.FechaVencimiento < hoy ? "Vencido" : "Activo";

        var contrato = new Contrato
        {
            IdEntidad = request.IdEntidad,
            IdPropiedad = request.IdPropiedad,
            IdUnidad = request.IdUnidad,
            TipoContrato = request.TipoContrato.Trim(),
            Condiciones = request.Condiciones.Trim(),
            FechaInicio = request.FechaInicio,
            FechaVencimiento = request.FechaVencimiento,
            Monto = request.MontoAlquilerMensual,
            MontoMantenimiento = request.MontoMantenimiento,
            Deposito = request.DepositoRequerido,
            DiaPago = request.DiaPago,
            AplicaITBIS = request.AplicaITBIS,
            Estado = estadoInicial
        };

        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();
        await SincronizarOcupacionAsync(contrato);
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Contratos", contrato.IdContrato, $"Contrato mensual RD${contrato.Monto:N2}");

        return CreatedAtAction(nameof(GetById), new { id = contrato.IdContrato }, await CargarResponseAsync(contrato.IdContrato));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.ContratosGestionar)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateContratoRequest request)
    {
        var contrato = await _context.Contratos.FirstOrDefaultAsync(c => c.IdContrato == id);
        if (contrato is null)
            return NotFound(new { mensaje = "Contrato no encontrado." });

        var validacion = await ValidarRequestAsync(request, id);
        if (validacion is not null)
            return BadRequest(new { mensaje = validacion });

        var propiedadAnterior = contrato.IdPropiedad;
        var unidadAnterior = contrato.IdUnidad;

        contrato.IdEntidad = request.IdEntidad;
        contrato.IdPropiedad = request.IdPropiedad;
        contrato.IdUnidad = request.IdUnidad;
        contrato.TipoContrato = request.TipoContrato.Trim();
        contrato.Condiciones = request.Condiciones.Trim();
        contrato.FechaInicio = request.FechaInicio;
        contrato.FechaVencimiento = request.FechaVencimiento;
        contrato.Monto = request.MontoAlquilerMensual;
        contrato.MontoMantenimiento = request.MontoMantenimiento;
        contrato.Deposito = request.DepositoRequerido;
        contrato.DiaPago = request.DiaPago;
        contrato.AplicaITBIS = request.AplicaITBIS;
        if (contrato.Estado != "Cancelado")
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            contrato.Estado = request.FechaInicio > hoy ? "Pendiente" : request.FechaVencimiento < hoy ? "Vencido" : "Activo";
        }

        await _context.SaveChangesAsync();
        await LiberarUbicacionSiCorrespondeAsync(propiedadAnterior, unidadAnterior, id);
        await SincronizarOcupacionAsync(contrato);
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Contratos", contrato.IdContrato);

        return Ok(await CargarResponseAsync(id));
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = Permissions.ContratosGestionar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
    {
        var nuevoEstado = EstadosValidos.FirstOrDefault(e => e.Equals(request.NuevoEstado?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (nuevoEstado is null)
            return BadRequest(new { mensaje = $"Estado inválido. Valores válidos: {string.Join(", ", EstadosValidos)}." });

        var contrato = await _context.Contratos.FindAsync(id);
        if (contrato is null)
            return NotFound(new { mensaje = "Contrato no encontrado." });

        if (nuevoEstado == "Activo")
        {
            if (!contrato.IdPropiedad.HasValue)
                return BadRequest(new { mensaje = "El contrato no tiene una propiedad válida." });

            var existeSolapamiento = await ExisteSolapamientoAsync(
                contrato.IdPropiedad.Value,
                contrato.IdUnidad,
                contrato.FechaInicio,
                contrato.FechaVencimiento,
                contrato.IdContrato);
            if (existeSolapamiento)
                return Conflict(new { mensaje = "La propiedad o unidad ya tiene otro contrato vigente durante esas fechas." });
        }

        contrato.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        if (nuevoEstado == "Activo")
            await SincronizarOcupacionAsync(contrato);
        else if (nuevoEstado is "Cancelado" or "Vencido")
            await LiberarUbicacionSiCorrespondeAsync(contrato.IdPropiedad, contrato.IdUnidad, contrato.IdContrato);

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "Contratos", contrato.IdContrato, $"Estado cambiado a {nuevoEstado}");
        return NoContent();
    }

    [HttpGet("{id:int}/resumen-facturacion")]
    [Authorize(Policy = Permissions.ContratosVer)]
    public async Task<IActionResult> ResumenFacturacion(int id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Facturas)
                .ThenInclude(f => f.Pagos)
            .FirstOrDefaultAsync(c => c.IdContrato == id);

        if (contrato is null)
            return NotFound(new { mensaje = "Contrato no encontrado." });

        var facturado = contrato.Facturas.Where(f => f.Estado != "ANULADA").Sum(f => f.Total);
        var pagado = contrato.Facturas.SelectMany(f => f.Pagos).Sum(p => p.Monto);

        return Ok(new
        {
            contrato.IdContrato,
            CodigoContrato = $"CTR-{contrato.IdContrato:D6}",
            Inquilino = contrato.Entidad.RazonSocial,
            Propiedad = contrato.Propiedad?.Direccion,
            Mensualidad = contrato.Monto,
            Mantenimiento = contrato.MontoMantenimiento ?? 0,
            TotalMensual = contrato.Monto + (contrato.MontoMantenimiento ?? 0),
            TotalFacturado = facturado,
            TotalPagado = pagado,
            SaldoPendiente = Math.Max(0, facturado - pagado)
        });
    }

    private async Task<string?> ValidarRequestAsync(CreateContratoRequest request, int? contratoId)
    {
        var inquilino = await _context.Entidades.FindAsync(request.IdEntidad);
        if (inquilino is null || !inquilino.Activo)
            return "El cliente/inquilino no existe o está inactivo.";
        if (inquilino.Tipo != "Cliente")
            return "La entidad seleccionada como inquilino debe ser de tipo Cliente.";

        var propiedad = await _context.Propiedades.FindAsync(request.IdPropiedad);
        if (propiedad is null || !propiedad.Activo)
            return "La propiedad no existe o está inactiva.";

        if (request.IdUnidad.HasValue)
        {
            var unidad = await _context.Unidades.FindAsync(request.IdUnidad.Value);
            if (unidad is null || !unidad.Activo || unidad.IdPropiedad != request.IdPropiedad)
                return "La unidad no existe, está inactiva o no pertenece a la propiedad seleccionada.";
        }

        if (request.FechaVencimiento <= request.FechaInicio)
            return "La fecha de vencimiento debe ser posterior a la fecha de inicio.";
        if (request.MontoAlquilerMensual <= 0)
            return "El monto mensual del alquiler debe ser mayor que cero.";
        if (request.DiaPago is < 1 or > 31)
            return "El día de pago debe estar entre 1 y 31.";

        if (await ExisteSolapamientoAsync(request.IdPropiedad, request.IdUnidad, request.FechaInicio, request.FechaVencimiento, contratoId))
            return "La propiedad o unidad ya tiene otro contrato vigente durante las fechas indicadas.";

        return null;
    }

    private async Task<bool> ExisteSolapamientoAsync(int idPropiedad, int? idUnidad, DateOnly inicio, DateOnly fin, int? excluirId)
    {
        return await _context.Contratos.AnyAsync(c =>
            (!excluirId.HasValue || c.IdContrato != excluirId.Value) &&
            c.IdPropiedad == idPropiedad &&
            c.IdUnidad == idUnidad &&
            c.Estado != "Cancelado" &&
            c.FechaInicio <= fin &&
            c.FechaVencimiento >= inicio);
    }

    private async Task SincronizarOcupacionAsync(Contrato contrato)
    {
        if (contrato.Estado != "Activo") return;

        if (contrato.IdUnidad.HasValue)
        {
            var unidad = await _context.Unidades.FindAsync(contrato.IdUnidad.Value);
            if (unidad is not null) unidad.Estado = "Alquilada";
        }
        else if (contrato.IdPropiedad.HasValue)
        {
            var propiedad = await _context.Propiedades.FindAsync(contrato.IdPropiedad.Value);
            if (propiedad is not null) propiedad.Estado = "Alquilada";
        }
    }

    private async Task LiberarUbicacionSiCorrespondeAsync(int? idPropiedad, int? idUnidad, int contratoExcluido)
    {
        if (!idPropiedad.HasValue) return;

        var existeOtroActivo = await _context.Contratos.AnyAsync(c =>
            c.IdContrato != contratoExcluido &&
            c.IdPropiedad == idPropiedad &&
            c.IdUnidad == idUnidad &&
            c.Estado == "Activo");
        if (existeOtroActivo) return;

        if (idUnidad.HasValue)
        {
            var unidad = await _context.Unidades.FindAsync(idUnidad.Value);
            if (unidad is not null) unidad.Estado = "Disponible";
        }
        else
        {
            var propiedad = await _context.Propiedades.FindAsync(idPropiedad.Value);
            if (propiedad is not null) propiedad.Estado = "Disponible";
        }
    }

    private async Task ActualizarContratosVencidosAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var vencidos = await _context.Contratos
            .Where(c => c.Estado == "Activo" && c.FechaVencimiento < hoy)
            .ToListAsync();
        if (vencidos.Count == 0) return;

        foreach (var contrato in vencidos)
        {
            contrato.Estado = "Vencido";
            await LiberarUbicacionSiCorrespondeAsync(contrato.IdPropiedad, contrato.IdUnidad, contrato.IdContrato);
        }
        await _context.SaveChangesAsync();
    }

    private async Task<ContratoResponse> CargarResponseAsync(int id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .SingleAsync(c => c.IdContrato == id);
        return MapResponse(contrato);
    }

    private static ContratoResponse MapResponse(Contrato c) => new()
    {
        IdContrato = c.IdContrato,
        IdEntidad = c.IdEntidad,
        RazonSocial = c.Entidad.RazonSocial,
        RncCedula = c.Entidad.RncCedula,
        IdPropiedad = c.IdPropiedad ?? 0,
        CodigoPropiedad = c.Propiedad?.Codigo,
        DireccionPropiedad = c.Propiedad?.Direccion,
        IdUnidad = c.IdUnidad,
        CodigoUnidad = c.Unidad?.Codigo,
        TipoContrato = c.TipoContrato,
        Condiciones = c.Condiciones,
        FechaInicio = c.FechaInicio,
        FechaVencimiento = c.FechaVencimiento,
        MontoAlquilerMensual = c.Monto,
        MontoMantenimiento = c.MontoMantenimiento ?? 0,
        DepositoRequerido = c.Deposito ?? 0,
        DiaPago = c.DiaPago,
        AplicaITBIS = c.AplicaITBIS,
        Estado = c.Estado
    };
}
