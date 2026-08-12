using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Acuerdos;
using SistemaFacturacion.Models;
using SistemaFacturacion.Services;

namespace SistemaFacturacion.Controllers;

[ApiController]
[Route("api/acuerdos-pago")]
[Authorize]
public class AcuerdosPagoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public AcuerdosPagoController(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? idContrato, [FromQuery] string? estado)
    {
        var query = BaseQuery();
        if (idContrato.HasValue) query = query.Where(a => a.IdContrato == idContrato.Value);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(a => a.Estado == estado);

        var acuerdos = await query.OrderByDescending(a => a.FechaCreacion).ToListAsync();
        return Ok(acuerdos.Select(MapResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var acuerdo = await BaseQuery().FirstOrDefaultAsync(a => a.IdAcuerdo == id);
        return acuerdo is null
            ? NotFound(new { mensaje = "Acuerdo de pago no encontrado." })
            : Ok(MapResponse(acuerdo));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAcuerdoPagoRequest request)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .FirstOrDefaultAsync(c => c.IdContrato == request.IdContrato);
        if (contrato is null || !contrato.IdPropiedad.HasValue)
            return BadRequest(new { mensaje = "El contrato no existe o no tiene propiedad." });
        if (contrato.Estado == "Cancelado")
            return BadRequest(new { mensaje = "No se puede crear un acuerdo para un contrato cancelado." });

        if (!request.IdFacturaOrigen.HasValue)
            return BadRequest(new { mensaje = "Debe seleccionar la factura pendiente que originará el acuerdo de pago." });

        FacturaCabecera? factura = null;
        if (request.IdFacturaOrigen.HasValue)
        {
            factura = await _context.FacturasCabecera
                .Include(f => f.Pagos)
                .FirstOrDefaultAsync(f => f.IdFactura == request.IdFacturaOrigen.Value && f.IdContrato == request.IdContrato);
            if (factura is null)
                return BadRequest(new { mensaje = "La factura de origen no pertenece al contrato." });
            if (!string.Equals(factura.TipoFactura, "CREDITO", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "Los acuerdos de pago solo pueden reestructurar facturas A CRÉDITO. Las facturas al contado deben pagarse en una sola exhibición." });

            var pendienteFactura = factura.Total - factura.Pagos.Sum(p => p.Monto);
            if (pendienteFactura <= 0)
                return BadRequest(new { mensaje = "La factura seleccionada no tiene saldo pendiente." });
            if (Math.Abs(request.MontoOriginal - pendienteFactura) > 0.009m)
                return BadRequest(new { mensaje = $"El acuerdo debe cubrir exactamente el saldo pendiente de la factura: RD${pendienteFactura:N2}." });

            var acuerdoExistente = await _context.AcuerdosPago.AnyAsync(a =>
                a.IdFacturaOrigen == factura.IdFactura && a.Estado == "Activo");
            if (acuerdoExistente)
                return Conflict(new { mensaje = "La factura ya tiene un acuerdo de pago activo." });
        }

        if (request.MontoAcordado != request.MontoOriginal)
            return BadRequest(new { mensaje = "Para este sistema académico, el monto acordado debe ser igual al saldo original. Cualquier descuento requeriría una nota de crédito." });

        var acuerdo = new AcuerdoPago
        {
            IdContrato = contrato.IdContrato,
            IdEntidad = contrato.IdEntidad,
            IdPropiedad = contrato.IdPropiedad.Value,
            IdFacturaOrigen = request.IdFacturaOrigen,
            MontoOriginal = request.MontoOriginal,
            MontoAcordado = request.MontoAcordado,
            CantidadCuotas = request.CantidadCuotas,
            MontoCuota = Math.Round(request.MontoAcordado / request.CantidadCuotas, 2),
            FechaInicio = request.FechaInicio,
            DiaPago = request.DiaPago,
            Estado = "Activo",
            Observaciones = request.Observaciones?.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        GenerarCuotas(acuerdo);
        _context.AcuerdosPago.Add(acuerdo);
        if (factura is not null)
            factura.Estado = "EN_ACUERDO";
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "AcuerdosPago", acuerdo.IdAcuerdo, $"RD${acuerdo.MontoAcordado:N2} en {acuerdo.CantidadCuotas} cuotas");

        var creado = await BaseQuery().SingleAsync(a => a.IdAcuerdo == acuerdo.IdAcuerdo);
        return CreatedAtAction(nameof(GetById), new { id = acuerdo.IdAcuerdo }, MapResponse(creado));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAcuerdoPagoRequest request)
    {
        var acuerdo = await _context.AcuerdosPago
            .Include(a => a.Cuotas)
            .Include(a => a.FacturaOrigen)
                .ThenInclude(f => f!.Pagos)
            .FirstOrDefaultAsync(a => a.IdAcuerdo == id);
        if (acuerdo is null)
            return NotFound(new { mensaje = "Acuerdo de pago no encontrado." });
        if (acuerdo.Estado != "Activo")
            return BadRequest(new { mensaje = "Solo se puede editar un acuerdo activo." });
        if (acuerdo.Cuotas.Any(c => c.MontoPagado > 0))
            return BadRequest(new { mensaje = "No se puede reestructurar un acuerdo que ya tiene cuotas pagadas." });
        if (request.IdContrato != acuerdo.IdContrato)
            return BadRequest(new { mensaje = "No se puede cambiar el contrato del acuerdo." });
        if (!request.IdFacturaOrigen.HasValue)
            return BadRequest(new { mensaje = "Debe seleccionar la factura pendiente que originará el acuerdo de pago." });
        if (request.MontoAcordado != request.MontoOriginal)
            return BadRequest(new { mensaje = "Para este sistema académico, el monto acordado debe ser igual al saldo original." });

        FacturaCabecera? facturaOrigen = null;
        if (request.IdFacturaOrigen.HasValue)
        {
            facturaOrigen = await _context.FacturasCabecera
                .Include(f => f.Pagos)
                .FirstOrDefaultAsync(f => f.IdFactura == request.IdFacturaOrigen.Value && f.IdContrato == acuerdo.IdContrato);
            if (facturaOrigen is null)
                return BadRequest(new { mensaje = "La factura de origen no pertenece al contrato." });
            if (!string.Equals(facturaOrigen.TipoFactura, "CREDITO", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "Los acuerdos de pago solo pueden reestructurar facturas A CRÉDITO." });

            var pendienteFactura = facturaOrigen.Total - facturaOrigen.Pagos.Sum(p => p.Monto);
            if (pendienteFactura <= 0)
                return BadRequest(new { mensaje = "La factura seleccionada no tiene saldo pendiente." });
            if (Math.Abs(request.MontoOriginal - pendienteFactura) > 0.009m)
                return BadRequest(new { mensaje = $"El acuerdo debe cubrir exactamente el saldo pendiente de la factura: RD${pendienteFactura:N2}." });

            var otroAcuerdo = await _context.AcuerdosPago.AnyAsync(a =>
                a.IdAcuerdo != acuerdo.IdAcuerdo &&
                a.IdFacturaOrigen == facturaOrigen.IdFactura &&
                a.Estado == "Activo");
            if (otroAcuerdo)
                return Conflict(new { mensaje = "La factura ya tiene otro acuerdo de pago activo." });
        }

        var facturaAnterior = acuerdo.FacturaOrigen;
        if (facturaAnterior is not null && facturaAnterior.IdFactura != request.IdFacturaOrigen.Value && facturaAnterior.Estado == "EN_ACUERDO")
        {
            var otroAcuerdoAnterior = await _context.AcuerdosPago.AnyAsync(a =>
                a.IdAcuerdo != acuerdo.IdAcuerdo &&
                a.IdFacturaOrigen == facturaAnterior.IdFactura &&
                a.Estado == "Activo");
            if (!otroAcuerdoAnterior)
            {
                var pagadoAnterior = facturaAnterior.Pagos.Sum(p => p.Monto);
                facturaAnterior.Estado = pagadoAnterior >= facturaAnterior.Total
                    ? "PAGADA"
                    : pagadoAnterior <= 0 ? "PENDIENTE" : "EN_PROCESO";
            }
        }

        acuerdo.IdFacturaOrigen = request.IdFacturaOrigen;
        acuerdo.MontoOriginal = request.MontoOriginal;
        acuerdo.MontoAcordado = request.MontoAcordado;
        acuerdo.CantidadCuotas = request.CantidadCuotas;
        acuerdo.MontoCuota = Math.Round(request.MontoAcordado / request.CantidadCuotas, 2);
        acuerdo.FechaInicio = request.FechaInicio;
        acuerdo.DiaPago = request.DiaPago;
        acuerdo.Observaciones = request.Observaciones?.Trim();

        _context.CuotasAcuerdoPago.RemoveRange(acuerdo.Cuotas);
        acuerdo.Cuotas = new List<CuotaAcuerdoPago>();
        GenerarCuotas(acuerdo);
        if (facturaOrigen is not null)
            facturaOrigen.Estado = "EN_ACUERDO";
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "EDITAR", "AcuerdosPago", acuerdo.IdAcuerdo);

        var actualizado = await BaseQuery().SingleAsync(a => a.IdAcuerdo == id);
        return Ok(MapResponse(actualizado));
    }

    [HttpPost("cuotas/{idCuota:int}/pagar")]
    public async Task<IActionResult> PagarCuota(int idCuota, [FromBody] PagarCuotaRequest request)
    {
        var cuota = await _context.CuotasAcuerdoPago
            .Include(c => c.Acuerdo)
            .ThenInclude(a => a.FacturaOrigen)
            .FirstOrDefaultAsync(c => c.IdCuotaAcuerdo == idCuota);
        if (cuota is null)
            return NotFound(new { mensaje = "Cuota no encontrada." });
        if (cuota.Acuerdo.Estado != "Activo")
            return BadRequest(new { mensaje = "El acuerdo no está activo." });

        var pendiente = cuota.Monto - cuota.MontoPagado;
        if (pendiente <= 0)
            return BadRequest(new { mensaje = "La cuota ya está pagada." });
        if (request.Monto > pendiente)
            return BadRequest(new { mensaje = $"El pago excede el saldo de la cuota (RD${pendiente:N2})." });

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            cuota.MontoPagado += request.Monto;
            cuota.Estado = cuota.MontoPagado >= cuota.Monto ? "Pagada" : "Parcial";

            var acuerdo = cuota.Acuerdo;
            Pago? pago = null;
            if (acuerdo.FacturaOrigen is not null)
            {
                var factura = acuerdo.FacturaOrigen;
                if (!string.Equals(factura.TipoFactura, "CREDITO", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { mensaje = "Este acuerdo está asociado a una factura que no es A CRÉDITO." });
                await _context.Entry(factura).Collection(f => f.Pagos).LoadAsync();
                await _context.Entry(factura).Collection(f => f.Movimientos).LoadAsync();
                var pagadoAntes = factura.Pagos.Sum(p => p.Monto);
                var pendienteFactura = Math.Max(0, factura.Total - pagadoAntes);
                if (request.Monto > pendienteFactura)
                    return BadRequest(new { mensaje = $"El pago excede el saldo pendiente de la factura (RD${pendienteFactura:N2})." });

                pago = new Pago
                {
                    IdFactura = factura.IdFactura,
                    IdContrato = acuerdo.IdContrato,
                    IdEntidad = acuerdo.IdEntidad,
                    IdPropiedad = acuerdo.IdPropiedad,
                    IdUnidad = factura.IdUnidad,
                    Monto = request.Monto,
                    FechaPago = DateTime.UtcNow,
                    MetodoPago = request.MetodoPago.Trim(),
                    Referencia = request.Referencia?.Trim(),
                    Notas = $"Cuota {cuota.NumeroCuota} del acuerdo ACP-{acuerdo.IdAcuerdo:D6}"
                };
                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                var porAplicar = request.Monto;
                foreach (var mov in factura.Movimientos.Where(m => m.MontoPendiente > 0).OrderBy(m => m.FechaVencimiento))
                {
                    var aplicado = Math.Min(mov.MontoPendiente, porAplicar);
                    mov.MontoPendiente -= aplicado;
                    porAplicar -= aplicado;
                    if (porAplicar <= 0) break;
                }

                var totalPagadoFactura = pagadoAntes + request.Monto;
                factura.Estado = totalPagadoFactura >= factura.Total ? "PAGADA" : "EN_ACUERDO";

                _context.AsientosContables.AddRange(
                    new AsientoContable
                    {
                        IdFacturaReferencia = factura.IdFactura,
                        IdCuentaContable = 2,
                        MontoDebito = request.Monto,
                        MontoCredito = 0,
                        Descripcion = $"Cobro cuota {cuota.NumeroCuota} acuerdo ACP-{acuerdo.IdAcuerdo:D6}"
                    },
                    new AsientoContable
                    {
                        IdFacturaReferencia = factura.IdFactura,
                        IdCuentaContable = 3,
                        MontoDebito = 0,
                        MontoCredito = request.Monto,
                        Descripcion = $"Disminución CxC acuerdo ACP-{acuerdo.IdAcuerdo:D6}"
                    });
            }

            _context.MovimientosCuenta.Add(new MovimientoCuenta
            {
                IdEntidad = acuerdo.IdEntidad,
                IdPropiedad = acuerdo.IdPropiedad,
                IdContrato = acuerdo.IdContrato,
                IdFactura = acuerdo.IdFacturaOrigen,
                IdPago = pago?.IdPago,
                Fecha = DateTime.UtcNow,
                TipoMovimiento = "PagoAcuerdo",
                Concepto = $"Pago cuota {cuota.NumeroCuota}/{acuerdo.CantidadCuotas} acuerdo ACP-{acuerdo.IdAcuerdo:D6}",
                Referencia = request.Referencia?.Trim(),
                Debito = 0,
                Credito = request.Monto
            });

            if (acuerdo.Cuotas.All(c => c.IdCuotaAcuerdo == cuota.IdCuotaAcuerdo ? cuota.MontoPagado >= cuota.Monto : c.Estado == "Pagada"))
                acuerdo.Estado = "Completado";

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            await _auditLog.LogFromContextAsync(HttpContext, "PAGAR", "AcuerdosPago", acuerdo.IdAcuerdo, $"Cuota {cuota.NumeroCuota}: RD${request.Monto:N2}");
            return Ok(new { mensaje = "Pago registrado.", cuota = cuota.NumeroCuota, cuota.Estado, cuota.MontoPagado });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var acuerdo = await _context.AcuerdosPago
            .Include(a => a.FacturaOrigen)
                .ThenInclude(f => f!.Pagos)
            .FirstOrDefaultAsync(a => a.IdAcuerdo == id);
        if (acuerdo is null)
            return NotFound(new { mensaje = "Acuerdo de pago no encontrado." });
        if (acuerdo.Estado == "Completado")
            return BadRequest(new { mensaje = "Un acuerdo completado no puede cancelarse." });

        acuerdo.Estado = "Cancelado";
        if (acuerdo.FacturaOrigen is not null && acuerdo.FacturaOrigen.Estado == "EN_ACUERDO")
        {
            var pagado = acuerdo.FacturaOrigen.Pagos.Sum(p => p.Monto);
            acuerdo.FacturaOrigen.Estado = pagado >= acuerdo.FacturaOrigen.Total
                ? "PAGADA"
                : pagado <= 0 ? "PENDIENTE" : "EN_PROCESO";
        }
        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "CANCELAR", "AcuerdosPago", acuerdo.IdAcuerdo);
        return NoContent();
    }

    private IQueryable<AcuerdoPago> BaseQuery() => _context.AcuerdosPago
        .Include(a => a.Contrato)
        .Include(a => a.Entidad)
        .Include(a => a.Propiedad)
        .Include(a => a.FacturaOrigen)
        .Include(a => a.Cuotas);

    private static void GenerarCuotas(AcuerdoPago acuerdo)
    {
        var montoBase = acuerdo.MontoCuota;
        var acumulado = 0m;
        for (var i = 1; i <= acuerdo.CantidadCuotas; i++)
        {
            var monto = i == acuerdo.CantidadCuotas ? acuerdo.MontoAcordado - acumulado : montoBase;
            acumulado += monto;
            var mes = acuerdo.FechaInicio.AddMonths(i - 1);
            var dia = Math.Min(acuerdo.DiaPago, DateTime.DaysInMonth(mes.Year, mes.Month));
            acuerdo.Cuotas.Add(new CuotaAcuerdoPago
            {
                NumeroCuota = i,
                FechaVencimiento = new DateOnly(mes.Year, mes.Month, dia),
                Monto = monto,
                MontoPagado = 0,
                Estado = "Pendiente"
            });
        }
    }

    private static AcuerdoPagoResponse MapResponse(AcuerdoPago a) => new()
    {
        IdAcuerdo = a.IdAcuerdo,
        IdContrato = a.IdContrato,
        CodigoContrato = $"CTR-{a.IdContrato:D6}",
        Inquilino = a.Entidad.RazonSocial,
        Propiedad = $"{a.Propiedad.Codigo} - {a.Propiedad.Direccion}",
        IdFacturaOrigen = a.IdFacturaOrigen,
        NumeroFacturaOrigen = a.FacturaOrigen?.NumeroECF,
        MontoOriginal = a.MontoOriginal,
        MontoAcordado = a.MontoAcordado,
        CantidadCuotas = a.CantidadCuotas,
        MontoCuota = a.MontoCuota,
        MontoPagado = a.Cuotas.Sum(c => c.MontoPagado),
        FechaInicio = a.FechaInicio,
        DiaPago = a.DiaPago,
        Estado = a.Estado,
        Observaciones = a.Observaciones,
        Cuotas = a.Cuotas.OrderBy(c => c.NumeroCuota).Select(c => new CuotaAcuerdoResponse
        {
            IdCuotaAcuerdo = c.IdCuotaAcuerdo,
            NumeroCuota = c.NumeroCuota,
            FechaVencimiento = c.FechaVencimiento,
            Monto = c.Monto,
            MontoPagado = c.MontoPagado,
            Estado = c.Estado
        }).ToList()
    };
}
