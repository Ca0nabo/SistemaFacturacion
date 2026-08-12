using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacion.Data;
using SistemaFacturacion.DTOs.Facturacion;
using SistemaFacturacion.DTOs.Pagos;
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
    public async Task<IActionResult> EmitirManual([FromBody] CreateFacturaRequest request)
    {
        var entidad = await _context.Entidades.FindAsync(request.IdEntidad);
        if (entidad is null || !entidad.Activo)
            return BadRequest(new { mensaje = "El cliente no existe o está inactivo." });
        if (request.Detalles.Count == 0)
            return BadRequest(new { mensaje = "La factura debe contener al menos un detalle." });

        request.TipoFactura = NormalizarTipoFactura(request.TipoFactura);
        if (!EsTipoFacturaValido(request.TipoFactura))
            return BadRequest(new { mensaje = "El tipo de factura debe ser CONTADO o CREDITO." });
        request.CantidadCuotas = NormalizarCuotas(request.TipoFactura, request.CantidadCuotas);
        if (request.TipoFactura == "CREDITO" && (request.CantidadCuotas < 2 || request.CantidadCuotas > 24))
            return BadRequest(new { mensaje = "Las facturas a crédito deben tener entre 2 y 24 cuotas." });

        Contrato? contrato = null;
        if (request.IdContrato.HasValue)
        {
            contrato = await _context.Contratos.FindAsync(request.IdContrato.Value);
            if (contrato is null)
                return BadRequest(new { mensaje = "El contrato seleccionado no existe." });
            if (contrato.IdEntidad != request.IdEntidad)
                return BadRequest(new { mensaje = "El contrato no pertenece al cliente seleccionado." });
            if (request.IdPropiedad.HasValue && contrato.IdPropiedad != request.IdPropiedad)
                return BadRequest(new { mensaje = "La propiedad seleccionada no corresponde al contrato." });
            if (request.IdUnidad.HasValue && contrato.IdUnidad != request.IdUnidad)
                return BadRequest(new { mensaje = "La unidad seleccionada no corresponde al contrato." });
        }

        var factura = await CrearFacturaAsync(
            entidad,
            contrato,
            request.IdPropiedad ?? contrato?.IdPropiedad,
            request.IdUnidad ?? contrato?.IdUnidad,
            "Manual",
            request.TipoFactura,
            request.CantidadCuotas,
            null,
            request.FechaVencimiento ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            request.AplicaITBIS,
            request.Detalles);

        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Facturacion", factura.IdFactura, $"Factura manual {request.TipoFactura} ({NormalizarCuotas(request.TipoFactura, request.CantidadCuotas)} cuota(s))");
        return CreatedAtAction(nameof(GetById), new { id = factura.IdFactura }, await MapResponseAsync(factura.IdFactura));
    }

    [HttpPost("mensual")]
    public async Task<IActionResult> GenerarMensual([FromBody] GenerarFacturaMensualRequest request)
    {
        var resultado = await GenerarFacturaMensualInternaAsync(request);
        if (!resultado.Exito)
            return resultado.Codigo == 409
                ? Conflict(new { mensaje = resultado.Mensaje })
                : BadRequest(new { mensaje = resultado.Mensaje });

        await _auditLog.LogFromContextAsync(HttpContext, "CREAR", "Facturacion", resultado.IdFactura, $"Factura mensual {request.Periodo} · {request.TipoFactura} · {request.CantidadCuotas} cuota(s)");
        return CreatedAtAction(nameof(GetById), new { id = resultado.IdFactura }, await MapResponseAsync(resultado.IdFactura!.Value));
    }

    [HttpPost("generar-mensuales")]
    public async Task<IActionResult> GenerarMensuales(
        [FromQuery] string? periodo,
        [FromQuery] string tipoFactura = "CONTADO",
        [FromQuery] int cantidadCuotas = 1)
    {
        periodo ??= DateTime.UtcNow.ToString("yyyy-MM");
        tipoFactura = NormalizarTipoFactura(tipoFactura);
        if (!EsTipoFacturaValido(tipoFactura))
            return BadRequest(new { mensaje = "El tipo de factura debe ser CONTADO o CREDITO." });
        if (tipoFactura == "CREDITO" && (cantidadCuotas < 2 || cantidadCuotas > 24))
            return BadRequest(new { mensaje = "Las facturas a crédito deben tener entre 2 y 24 cuotas." });
        if (tipoFactura == "CONTADO") cantidadCuotas = 1;
        if (!TryParsePeriodo(periodo, out var inicioPeriodo, out _))
            return BadRequest(new { mensaje = "El período debe tener formato AAAA-MM." });

        var contratos = await _context.Contratos
            .Where(c => c.Estado != "Cancelado" &&
                        c.FechaInicio <= inicioPeriodo.AddMonths(1).AddDays(-1) &&
                        c.FechaVencimiento >= inicioPeriodo)
            .Select(c => c.IdContrato)
            .ToListAsync();

        var creadas = new List<int>();
        var omitidas = new List<object>();
        foreach (var idContrato in contratos)
        {
            var r = await GenerarFacturaMensualInternaAsync(new GenerarFacturaMensualRequest
            {
                IdContrato = idContrato,
                Periodo = periodo,
                TipoFactura = tipoFactura,
                CantidadCuotas = cantidadCuotas
            });

            if (r.Exito && r.IdFactura.HasValue)
                creadas.Add(r.IdFactura.Value);
            else
                omitidas.Add(new { idContrato, r.Mensaje });
        }

        return Ok(new
        {
            periodo,
            tipoFactura,
            cantidadCuotas,
            contratosEvaluados = contratos.Count,
            facturasCreadas = creadas.Count,
            idsFacturas = creadas,
            omitidas
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? idContrato,
        [FromQuery] int? idEntidad,
        [FromQuery] int? idPropiedad,
        [FromQuery] string? estado,
        [FromQuery] string? periodo,
        [FromQuery] string? tipoFactura)
    {
        var query = _context.FacturasCabecera
            .Include(f => f.Entidad)
            .Include(f => f.Contrato)
            .Include(f => f.Propiedad)
            .Include(f => f.Unidad)
            .Include(f => f.Detalles)
            .Include(f => f.Pagos)
            .Include(f => f.Movimientos)
            .AsQueryable();

        if (idContrato.HasValue) query = query.Where(f => f.IdContrato == idContrato.Value);
        if (idEntidad.HasValue) query = query.Where(f => f.IdEntidad == idEntidad.Value);
        if (idPropiedad.HasValue) query = query.Where(f => f.IdPropiedad == idPropiedad.Value);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(f => f.Estado == estado);
        if (!string.IsNullOrWhiteSpace(periodo)) query = query.Where(f => f.PeriodoFacturado == periodo);
        if (!string.IsNullOrWhiteSpace(tipoFactura))
        {
            var tipoNormalizado = NormalizarTipoFactura(tipoFactura);
            query = query.Where(f => f.TipoFactura == tipoNormalizado);
        }

        var facturas = await query.OrderByDescending(f => f.FechaEmision).ToListAsync();
        return Ok(facturas.Select(MapResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var factura = await _context.FacturasCabecera
            .Include(f => f.Entidad)
            .Include(f => f.Contrato)
            .Include(f => f.Propiedad)
            .Include(f => f.Unidad)
            .Include(f => f.Detalles)
            .Include(f => f.Pagos)
            .Include(f => f.Movimientos)
            .FirstOrDefaultAsync(f => f.IdFactura == id);

        return factura is null
            ? NotFound(new { mensaje = "Factura no encontrada." })
            : Ok(MapResponse(factura));
    }

    [HttpPost("{id:int}/pagos")]
    public async Task<IActionResult> RegistrarPago(int id, [FromBody] RegistrarPagoRequest request)
    {
        var factura = await _context.FacturasCabecera
            .Include(f => f.Pagos)
            .Include(f => f.Movimientos)
            .FirstOrDefaultAsync(f => f.IdFactura == id);

        if (factura is null)
            return NotFound(new { mensaje = "Factura no encontrada." });
        if (factura.Estado == "ANULADA")
            return BadRequest(new { mensaje = "No se pueden registrar pagos en una factura anulada." });
        if (await _context.AcuerdosPago.AnyAsync(a => a.IdFacturaOrigen == factura.IdFactura && a.Estado == "Activo"))
            return BadRequest(new { mensaje = "La factura tiene un acuerdo de pago activo. Registre el pago desde las cuotas del acuerdo." });

        var pagadoAntes = factura.Pagos.Sum(p => p.Monto);
        var pendienteAntes = Math.Max(0, factura.Total - pagadoAntes);
        if (pendienteAntes <= 0)
            return BadRequest(new { mensaje = "La factura no tiene saldo pendiente." });

        var tipoFactura = NormalizarTipoFactura(factura.TipoFactura);
        if (!EsTipoFacturaValido(tipoFactura))
            return BadRequest(new { mensaje = "La factura no tiene una condición de pago válida." });

        if (tipoFactura == "CONTADO" && Math.Abs(request.Monto - pendienteAntes) > 0.009m)
            return BadRequest(new { mensaje = $"Una factura al contado exige un único pago por el saldo completo de RD${pendienteAntes:N2}. No se permiten abonos parciales." });

        if (request.Monto > pendienteAntes)
            return BadRequest(new { mensaje = $"El pago excede el saldo pendiente de RD${pendienteAntes:N2}." });

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
                Monto = request.Monto,
                FechaPago = request.FechaPago?.ToUniversalTime() ?? DateTime.UtcNow,
                MetodoPago = request.MetodoPago.Trim(),
                Referencia = request.Referencia?.Trim(),
                Notas = request.Notas?.Trim()
            };
            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            var porAplicar = request.Monto;
            foreach (var movimiento in factura.Movimientos.Where(m => m.MontoPendiente > 0).OrderBy(m => m.FechaVencimiento))
            {
                var aplicado = Math.Min(movimiento.MontoPendiente, porAplicar);
                movimiento.MontoPendiente -= aplicado;
                porAplicar -= aplicado;
                if (porAplicar <= 0) break;
            }

            var saldo = Math.Max(0, pendienteAntes - request.Monto);
            factura.Estado = saldo <= 0.009m
                ? "PAGADA"
                : tipoFactura == "CREDITO" ? "EN_PROCESO" : "EMITIDA";

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

            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 2,
                MontoDebito = pago.Monto,
                MontoCredito = 0,
                Descripcion = $"Cobro factura {factura.NumeroECF}"
            });
            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 3,
                MontoDebito = 0,
                MontoCredito = pago.Monto,
                Descripcion = $"Disminución CxC factura {factura.NumeroECF}"
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _auditLog.LogFromContextAsync(HttpContext, "PAGAR", "Facturacion", factura.IdFactura, $"{tipoFactura} · RD${pago.Monto:N2} · saldo RD${saldo:N2}");

            return Ok(new PagoResponse
            {
                IdPago = pago.IdPago,
                IdFactura = factura.IdFactura,
                NumeroFactura = factura.NumeroECF,
                Monto = pago.Monto,
                FechaPago = pago.FechaPago,
                MetodoPago = pago.MetodoPago,
                Referencia = pago.Referencia,
                Notas = pago.Notas,
                SaldoPendienteFactura = saldo
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        var factura = await _context.FacturasCabecera
            .Include(f => f.Pagos)
            .Include(f => f.Movimientos)
            .FirstOrDefaultAsync(f => f.IdFactura == id);

        if (factura is null)
            return NotFound(new { mensaje = "Factura no encontrada." });
        if (factura.Estado == "ANULADA")
            return BadRequest(new { mensaje = "La factura ya está anulada." });
        if (factura.Pagos.Any())
            return BadRequest(new { mensaje = "No se puede anular una factura que ya tiene pagos. Registre un ajuste o devolución." });

        factura.Estado = "ANULADA";
        foreach (var movimiento in factura.Movimientos)
            movimiento.MontoPendiente = 0;

        _context.MovimientosCuenta.Add(new MovimientoCuenta
        {
            IdEntidad = factura.IdEntidad,
            IdPropiedad = factura.IdPropiedad,
            IdUnidad = factura.IdUnidad,
            IdContrato = factura.IdContrato,
            IdFactura = factura.IdFactura,
            TipoMovimiento = "Anulación",
            Concepto = $"Anulación de {factura.NumeroECF}",
            Debito = 0,
            Credito = factura.Total
        });

        await _context.SaveChangesAsync();
        await _auditLog.LogFromContextAsync(HttpContext, "ANULAR", "Facturacion", factura.IdFactura);
        return NoContent();
    }

    private async Task<(bool Exito, int Codigo, string Mensaje, int? IdFactura)> GenerarFacturaMensualInternaAsync(GenerarFacturaMensualRequest request)
    {
        if (!TryParsePeriodo(request.Periodo, out var inicioPeriodo, out var finPeriodo))
            return (false, 400, "El período debe tener formato AAAA-MM.", null);

        request.TipoFactura = NormalizarTipoFactura(request.TipoFactura);
        if (!EsTipoFacturaValido(request.TipoFactura))
            return (false, 400, "El tipo de factura debe ser CONTADO o CREDITO.", null);
        request.CantidadCuotas = NormalizarCuotas(request.TipoFactura, request.CantidadCuotas);
        if (request.TipoFactura == "CREDITO" && (request.CantidadCuotas < 2 || request.CantidadCuotas > 24))
            return (false, 400, "Las facturas a crédito deben tener entre 2 y 24 cuotas.", null);

        var ahoraLocal = DateTime.Now;
        var inicioPeriodoActual = new DateOnly(ahoraLocal.Year, ahoraLocal.Month, 1);
        if (inicioPeriodo > inicioPeriodoActual)
            return (false, 400, "No se pueden generar facturas para períodos futuros.", null);

        var contrato = await _context.Contratos
            .Include(c => c.Entidad)
            .Include(c => c.Propiedad)
            .Include(c => c.Unidad)
            .FirstOrDefaultAsync(c => c.IdContrato == request.IdContrato);

        if (contrato is null)
            return (false, 400, "El contrato no existe.", null);
        if (contrato.Estado == "Cancelado")
            return (false, 400, "No se puede facturar un contrato cancelado.", null);
        if (!contrato.IdPropiedad.HasValue || contrato.Propiedad is null)
            return (false, 400, "El contrato no tiene una propiedad válida.", null);
        if (contrato.FechaInicio > finPeriodo || contrato.FechaVencimiento < inicioPeriodo)
            return (false, 400, "El contrato no está vigente durante el período indicado.", null);

        var duplicada = await _context.FacturasCabecera.AnyAsync(f =>
            f.IdContrato == request.IdContrato &&
            f.PeriodoFacturado == request.Periodo &&
            f.OrigenFactura == "AlquilerMensual" &&
            f.Estado != "ANULADA");
        if (duplicada)
            return (false, 409, "Ya existe una factura mensual para este contrato y período.", null);

        var dia = Math.Min(contrato.DiaPago, DateTime.DaysInMonth(inicioPeriodo.Year, inicioPeriodo.Month));
        var vencimiento = request.FechaVencimiento ?? new DateOnly(inicioPeriodo.Year, inicioPeriodo.Month, dia);
        if (vencimiento < contrato.FechaInicio)
            vencimiento = contrato.FechaInicio;
        var detalles = new List<FacturaDetalleItem>
        {
            new()
            {
                DescripcionItem = $"Alquiler mensual {request.Periodo} - {contrato.Propiedad.Codigo}",
                Cantidad = 1,
                Precio = contrato.Monto
            }
        };

        if ((contrato.MontoMantenimiento ?? 0) > 0)
        {
            detalles.Add(new FacturaDetalleItem
            {
                DescripcionItem = $"Mantenimiento mensual {request.Periodo}",
                Cantidad = 1,
                Precio = contrato.MontoMantenimiento!.Value
            });
        }

        var factura = await CrearFacturaAsync(
            contrato.Entidad,
            contrato,
            contrato.IdPropiedad,
            contrato.IdUnidad,
            "AlquilerMensual",
            request.TipoFactura,
            request.CantidadCuotas,
            request.Periodo,
            vencimiento,
            contrato.AplicaITBIS,
            detalles);

        var mensaje = request.TipoFactura == "CONTADO"
            ? "Factura mensual al contado generada."
            : $"Factura mensual a crédito generada en {request.CantidadCuotas} cuotas.";
        return (true, 201, mensaje, factura.IdFactura);
    }

    private async Task<FacturaCabecera> CrearFacturaAsync(
        Entidad entidad,
        Contrato? contrato,
        int? idPropiedad,
        int? idUnidad,
        string origenFactura,
        string tipoFactura,
        int cantidadCuotas,
        string? periodo,
        DateOnly fechaVencimiento,
        bool aplicaItbis,
        IReadOnlyCollection<FacturaDetalleItem> detalles)
    {
        var subtotal = detalles.Sum(d => d.Cantidad * d.Precio);
        var parametros = await _context.ParametrosEmpresa.FirstOrDefaultAsync();
        var tasaItbis = parametros?.PorcentajeITBIS ?? 0.18m;
        var itbis = aplicaItbis ? Math.Round(subtotal * tasaItbis, 2) : 0m;
        var total = subtotal + itbis;
        tipoFactura = NormalizarTipoFactura(tipoFactura);
        if (!EsTipoFacturaValido(tipoFactura))
            throw new ArgumentException("El tipo de factura debe ser CONTADO o CREDITO.");
        cantidadCuotas = NormalizarCuotas(tipoFactura, cantidadCuotas);
        if (tipoFactura == "CREDITO" && (cantidadCuotas < 2 || cantidadCuotas > 24))
            throw new ArgumentException("Las facturas a crédito deben tener entre 2 y 24 cuotas.");

        var factura = new FacturaCabecera
        {
            IdEntidad = entidad.IdEntidad,
            IdContrato = contrato?.IdContrato,
            IdPropiedad = idPropiedad,
            IdUnidad = idUnidad,
            NumeroECF = $"TMP-{Guid.NewGuid():N}",
            FechaEmision = DateTime.UtcNow,
            FechaVencimiento = fechaVencimiento,
            TipoFactura = tipoFactura,
            OrigenFactura = origenFactura,
            PeriodoFacturado = periodo,
            AplicaITBIS = aplicaItbis,
            Subtotal = subtotal,
            Itbis = itbis,
            Total = total,
            Estado = tipoFactura == "CREDITO" ? "PENDIENTE" : "EMITIDA"
        };

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.FacturasCabecera.Add(factura);
            await _context.SaveChangesAsync();

            factura.NumeroECF = origenFactura == "AlquilerMensual"
                ? $"FAC-{periodo?.Replace("-", string.Empty)}-{factura.IdFactura:D6}"
                : $"FAC-{DateTime.UtcNow:yyyyMM}-{factura.IdFactura:D6}";
            factura.FirmaDGII = await _dgiiMock.FirmarECFAsync(factura.IdFactura, total);

            foreach (var item in detalles)
            {
                _context.FacturasDetalle.Add(new FacturaDetalle
                {
                    IdFactura = factura.IdFactura,
                    DescripcionItem = item.DescripcionItem.Trim(),
                    Cantidad = item.Cantidad,
                    Precio = item.Precio
                });
            }

            var montoBaseCuota = Math.Round(total / cantidadCuotas, 2);
            var acumuladoCuotas = 0m;
            for (var i = 1; i <= cantidadCuotas; i++)
            {
                var montoCuota = i == cantidadCuotas ? total - acumuladoCuotas : montoBaseCuota;
                acumuladoCuotas += montoCuota;
                var vencimientoCuota = i == 1 ? fechaVencimiento : fechaVencimiento.AddMonths(i - 1);

                _context.MovimientosCx.Add(new MovimientosCx
                {
                    IdFactura = factura.IdFactura,
                    IdPropiedad = factura.IdPropiedad,
                    IdUnidad = factura.IdUnidad,
                    Tipo = "CxC",
                    MontoOriginal = montoCuota,
                    MontoPendiente = montoCuota,
                    FechaVencimiento = vencimientoCuota,
                    NumeroCuota = i,
                    TotalCuotas = cantidadCuotas
                });
            }

            _context.MovimientosCuenta.Add(new MovimientoCuenta
            {
                IdEntidad = factura.IdEntidad,
                IdPropiedad = factura.IdPropiedad,
                IdUnidad = factura.IdUnidad,
                IdContrato = factura.IdContrato,
                IdFactura = factura.IdFactura,
                Fecha = factura.FechaEmision,
                TipoMovimiento = "Factura",
                Concepto = origenFactura == "AlquilerMensual"
                    ? $"Factura mensual {periodo} · {tipoFactura}"
                    : $"Factura manual · {tipoFactura}",
                Referencia = factura.NumeroECF,
                Debito = total,
                Credito = 0
            });

            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 3,
                MontoDebito = total,
                MontoCredito = 0,
                Descripcion = $"Factura {factura.NumeroECF} - Cuentas por cobrar"
            });
            _context.AsientosContables.Add(new AsientoContable
            {
                IdFacturaReferencia = factura.IdFactura,
                IdCuentaContable = 5,
                MontoDebito = 0,
                MontoCredito = subtotal,
                Descripcion = $"Factura {factura.NumeroECF} - Ingresos por alquiler"
            });
            if (itbis > 0)
            {
                _context.AsientosContables.Add(new AsientoContable
                {
                    IdFacturaReferencia = factura.IdFactura,
                    IdCuentaContable = 6,
                    MontoDebito = 0,
                    MontoCredito = itbis,
                    Descripcion = $"Factura {factura.NumeroECF} - ITBIS por pagar"
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return factura;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<FacturaResponse> MapResponseAsync(int id)
    {
        var factura = await _context.FacturasCabecera
            .Include(f => f.Entidad)
            .Include(f => f.Contrato)
            .Include(f => f.Propiedad)
            .Include(f => f.Unidad)
            .Include(f => f.Detalles)
            .Include(f => f.Pagos)
            .Include(f => f.Movimientos)
            .SingleAsync(f => f.IdFactura == id);
        return MapResponse(factura);
    }

    private static FacturaResponse MapResponse(FacturaCabecera f)
    {
        var pagado = f.Pagos.Sum(p => p.Monto);
        return new FacturaResponse
        {
            IdFactura = f.IdFactura,
            NumeroECF = f.NumeroECF,
            IdContrato = f.IdContrato,
            CodigoContrato = f.IdContrato.HasValue ? $"CTR-{f.IdContrato.Value:D6}" : null,
            RazonSocial = f.Entidad.RazonSocial,
            RncCedula = f.Entidad.RncCedula,
            IdPropiedad = f.IdPropiedad,
            CodigoPropiedad = f.Propiedad?.Codigo,
            DireccionPropiedad = f.Propiedad?.Direccion,
            IdUnidad = f.IdUnidad,
            CodigoUnidad = f.Unidad?.Codigo,
            FechaEmision = f.FechaEmision,
            FechaVencimiento = f.FechaVencimiento,
            ProximoVencimiento = f.Movimientos
                .Where(m => m.Tipo == "CxC" && m.MontoPendiente > 0)
                .OrderBy(m => m.FechaVencimiento)
                .Select(m => (DateOnly?)m.FechaVencimiento)
                .FirstOrDefault(),
            TieneCuotaVencida = f.Movimientos.Any(m => m.Tipo == "CxC" && m.MontoPendiente > 0 && m.FechaVencimiento < DateOnly.FromDateTime(DateTime.Now)),
            TipoFactura = NormalizarTipoFactura(f.TipoFactura),
            OrigenFactura = f.OrigenFactura,
            CantidadCuotas = f.Movimientos
                .Where(m => m.Tipo == "CxC")
                .Select(m => m.TotalCuotas ?? 1)
                .DefaultIfEmpty(1)
                .Max(),
            PeriodoFacturado = f.PeriodoFacturado,
            AplicaITBIS = f.AplicaITBIS,
            Subtotal = f.Subtotal,
            Itbis = f.Itbis,
            Total = f.Total,
            MontoPagado = pagado,
            MontoPendiente = Math.Max(0, f.Total - pagado),
            Estado = f.Estado,
            FirmaDGII = f.FirmaDGII ?? string.Empty,
            Detalles = f.Detalles.Select(d => new FacturaDetalleResponse
            {
                DescripcionItem = d.DescripcionItem,
                Cantidad = d.Cantidad,
                Precio = d.Precio,
                Subtotal = d.Cantidad * d.Precio
            }).ToList(),
            Cuotas = f.Movimientos
                .Where(m => m.Tipo == "CxC")
                .OrderBy(m => m.NumeroCuota ?? 1)
                .Select(m => new FacturaCuotaResponse
                {
                    NumeroCuota = m.NumeroCuota ?? 1,
                    TotalCuotas = m.TotalCuotas ?? 1,
                    FechaVencimiento = m.FechaVencimiento,
                    Monto = m.MontoOriginal,
                    Pendiente = m.MontoPendiente
                }).ToList()
        };
    }

    private static string NormalizarTipoFactura(string? tipo)
        => string.Equals(tipo?.Trim(), "CREDITO", StringComparison.OrdinalIgnoreCase)
            ? "CREDITO"
            : string.Equals(tipo?.Trim(), "CONTADO", StringComparison.OrdinalIgnoreCase)
                ? "CONTADO"
                : (tipo?.Trim().ToUpperInvariant() ?? string.Empty);

    private static bool EsTipoFacturaValido(string? tipo)
        => tipo is "CONTADO" or "CREDITO";

    private static int NormalizarCuotas(string tipoFactura, int cantidadCuotas)
        => tipoFactura == "CONTADO" ? 1 : cantidadCuotas;

    private static bool TryParsePeriodo(string periodo, out DateOnly inicio, out DateOnly fin)
    {
        inicio = default;
        fin = default;
        if (!DateTime.TryParseExact(periodo, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var fecha))
            return false;
        inicio = new DateOnly(fecha.Year, fecha.Month, 1);
        fin = inicio.AddMonths(1).AddDays(-1);
        return true;
    }
}
