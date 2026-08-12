let facturasCache = [];
let contratosFacturacionCache = [];
let facturaPagoActual = null;
let facturaActualParaImprimir = null;
window.facturaContratoPreseleccionado = null;
window.acuerdoFacturaPreseleccionada = null;

function initFacturas() {
    onView('facturas', loadFacturas);
    onView('factura-form', initFacturaFormView);

    document.getElementById('btn-nueva-factura').addEventListener('click', () => {
        window.facturaContratoPreseleccionado = null;
        navigateTo('factura-form');
    });
    document.getElementById('btn-volver-facturas').addEventListener('click', () => navigateTo('facturas'));
    document.getElementById('btn-filtrar-facturas').addEventListener('click', loadFacturas);
    document.getElementById('btn-generar-lote').addEventListener('click', abrirLoteFacturasModal);
    document.getElementById('factura-form').addEventListener('submit', generarFacturaMensual);
    document.getElementById('factura-contrato').addEventListener('change', actualizarResumenFacturaContrato);
    document.getElementById('factura-periodo').addEventListener('change', actualizarResumenFacturaContrato);
    document.getElementById('factura-tipo').addEventListener('change', actualizarTipoFacturaForm);
    document.getElementById('factura-cuotas').addEventListener('input', actualizarTipoFacturaForm);

    document.getElementById('btn-cerrar-factura-modal').addEventListener('click', cerrarFacturaModal);
    document.getElementById('btn-cerrar-factura-modal-2').addEventListener('click', cerrarFacturaModal);
    document.querySelector('[data-close-factura-modal]').addEventListener('click', cerrarFacturaModal);
    document.getElementById('btn-imprimir-factura').addEventListener('click', imprimirFacturaActual);

    document.getElementById('pago-form').addEventListener('submit', guardarPagoFactura);
    document.getElementById('pago-monto').addEventListener('input', actualizarResumenPagoFactura);
    document.getElementById('btn-cerrar-pago-modal').addEventListener('click', cerrarPagoModal);
    document.getElementById('btn-cancelar-pago').addEventListener('click', cerrarPagoModal);
    document.querySelector('[data-close-pago-modal]').addEventListener('click', cerrarPagoModal);

    document.getElementById('lote-tipo').addEventListener('change', actualizarLoteTipoUI);
    document.getElementById('lote-cuotas').addEventListener('input', actualizarLoteTipoUI);
    document.getElementById('btn-cerrar-lote-modal').addEventListener('click', cerrarLoteFacturasModal);
    document.getElementById('btn-cancelar-lote').addEventListener('click', cerrarLoteFacturasModal);
    document.querySelector('[data-close-lote-modal]').addEventListener('click', cerrarLoteFacturasModal);
    document.getElementById('btn-confirmar-lote').addEventListener('click', generarFacturasDelMes);
}

function nombreTipoFactura(tipo) {
    return String(tipo || '').toUpperCase() === 'CREDITO' ? 'A crédito' : 'Al contado';
}

function badgeTipoFactura(tipo) {
    const credito = String(tipo || '').toUpperCase() === 'CREDITO';
    return `<span class="invoice-type-badge ${credito ? 'invoice-type-credit' : 'invoice-type-cash'}">${credito ? 'A crédito' : 'Al contado'}</span>`;
}

async function loadFacturas() {
    const tbody = document.getElementById('facturas-body');
    tbody.innerHTML = '<tr><td colspan="11" class="loading">Cargando facturas...</td></tr>';

    const qs = queryString({
        estado: document.getElementById('facturas-filter-estado').value,
        periodo: document.getElementById('facturas-filter-periodo').value,
        tipoFactura: document.getElementById('facturas-filter-tipo').value
    });

    try {
        facturasCache = await apiGet(`/facturacion${qs}`);
        if (!facturasCache.length) {
            tbody.innerHTML = '<tr><td colspan="11" class="empty-state">No hay facturas para los filtros seleccionados.</td></tr>';
            return;
        }

        const today = todayIso();
        tbody.innerHTML = facturasCache.map(f => {
            const visualStatus = !['PAGADA', 'ANULADA'].includes(f.estado) && (f.tieneCuotaVencida || (f.fechaVencimiento && f.fechaVencimiento < today && f.tipoFactura === 'CONTADO'))
                ? 'VENCIDA'
                : f.estado;
            const plan = String(f.tipoFactura).toUpperCase() === 'CREDITO'
                ? `${f.cantidadCuotas || 1} cuotas`
                : 'Pago único';
            const tieneAcuerdo = f.estado === 'EN_ACUERDO';
            const puedePagar = Number(f.montoPendiente) > 0 && !['ANULADA', 'EN_ACUERDO'].includes(f.estado);
            const puedeAnular = f.estado !== 'ANULADA' && f.estado !== 'EN_ACUERDO' && Number(f.montoPagado) === 0;

            return `<tr>
                <td><strong>${escapeHtml(f.numeroECF)}</strong><br><span class="muted">${formatearFechaHora(f.fechaEmision)}</span></td>
                <td>${badgeTipoFactura(f.tipoFactura)}<br><span class="muted">${escapeHtml(plan)}</span></td>
                <td>${escapeHtml(f.codigoContrato || 'Manual')}<br><span class="muted">${escapeHtml(f.periodoFacturado || f.origenFactura || 'Manual')}</span></td>
                <td>${escapeHtml(f.razonSocial)}</td>
                <td>${escapeHtml(f.codigoPropiedad || '—')} ${f.codigoUnidad ? `/ ${escapeHtml(f.codigoUnidad)}` : ''}<br><span class="muted">${escapeHtml(f.direccionPropiedad || '')}</span></td>
                <td>${formatearFecha(f.proximoVencimiento || f.fechaVencimiento)}</td>
                <td>${dinero(f.total)}</td>
                <td>${dinero(f.montoPagado)}</td>
                <td><strong>${dinero(f.montoPendiente)}</strong></td>
                <td><span class="status-badge ${statusClass(visualStatus)}">${escapeHtml(visualStatus)}</span></td>
                <td class="acciones">
                    <button class="btn btn-sm btn-secondary" onclick="verFactura(${f.idFactura})">Ver</button>
                    ${puedePagar ? `<button class="btn btn-sm btn-success" onclick="registrarPagoFactura(${f.idFactura})">Registrar pago</button>` : ''}
                    ${tieneAcuerdo ? `<button class="btn btn-sm btn-primary" onclick="pagarDesdeAcuerdoFactura(${f.idFactura})">Pagar desde Acuerdos</button>` : ''}
                    ${puedeAnular ? `<button class="btn btn-sm btn-danger" onclick="anularFactura(${f.idFactura})">Anular</button>` : ''}
                </td>
            </tr>`;
        }).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="11" class="table-error">${escapeHtml(error.message)}</td></tr>`;
    }
}

async function initFacturaFormView() {
    const form = document.getElementById('factura-form');
    form.reset();
    const periodoActual = currentPeriod();
    const periodoInput = document.getElementById('factura-periodo');
    periodoInput.value = periodoActual;
    periodoInput.max = periodoActual;
    document.getElementById('factura-tipo').value = 'CONTADO';
    document.getElementById('factura-cuotas').value = 3;
    document.getElementById('factura-error').classList.add('hidden');

    try {
        contratosFacturacionCache = (await apiGet('/contratos')).filter(c => c.estado !== 'Cancelado');
        const select = document.getElementById('factura-contrato');
        select.innerHTML = '<option value="">Seleccione un contrato...</option>' + contratosFacturacionCache
            .map(c => `<option value="${c.idContrato}">${escapeHtml(c.codigoContrato)} · ${escapeHtml(c.razonSocial)} · ${escapeHtml(c.codigoPropiedad || '')} · ${dinero(c.totalMensual)}</option>`)
            .join('');

        if (window.facturaContratoPreseleccionado) {
            select.value = window.facturaContratoPreseleccionado;
            window.facturaContratoPreseleccionado = null;
        }
        actualizarTipoFacturaForm();
        actualizarResumenFacturaContrato();
    } catch (error) {
        document.getElementById('factura-error').textContent = error.message;
        document.getElementById('factura-error').classList.remove('hidden');
    }
}

function actualizarTipoFacturaForm() {
    const tipo = document.getElementById('factura-tipo').value;
    const cuotasGroup = document.getElementById('factura-cuotas-group');
    const cuotas = document.getElementById('factura-cuotas');
    const resumen = document.getElementById('factura-condicion-resumen');

    if (tipo === 'CREDITO') {
        cuotasGroup.classList.remove('hidden');
        cuotas.required = true;
        if (Number(cuotas.value) < 2) cuotas.value = 3;
        resumen.className = 'payment-rule payment-rule-credit';
        resumen.innerHTML = `<strong>A crédito:</strong> la deuda se dividirá en ${Number(cuotas.value) || 3} cuotas mensuales y se permitirán abonos hasta completar el balance.`;
    } else {
        cuotasGroup.classList.add('hidden');
        cuotas.required = false;
        resumen.className = 'payment-rule payment-rule-cash';
        resumen.innerHTML = '<strong>Al contado:</strong> el pago debe cubrir el 100% del saldo en una sola operación.';
    }
}

function actualizarResumenFacturaContrato() {
    const id = Number(document.getElementById('factura-contrato').value);
    const c = contratosFacturacionCache.find(x => x.idContrato === id);
    const target = document.getElementById('factura-resumen-contrato');
    if (!c) {
        target.textContent = 'Seleccione un contrato.';
        return;
    }

    target.innerHTML = `<strong>${escapeHtml(c.codigoPropiedad || '')}</strong> · Alquiler ${dinero(c.montoAlquilerMensual)} + mantenimiento ${dinero(c.montoMantenimiento)} = <strong>${dinero(c.totalMensual)}</strong>${c.aplicaITBIS ? ' + ITBIS' : ''}. Día de pago: ${c.diaPago}.`;
    const period = document.getElementById('factura-periodo').value || currentPeriod();
    const [year, month] = period.split('-').map(Number);
    if (year && month) {
        const day = Math.min(c.diaPago, new Date(year, month, 0).getDate());
        document.getElementById('factura-vencimiento').value = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    }
}

async function generarFacturaMensual(event) {
    event.preventDefault();
    const error = document.getElementById('factura-error');
    const button = document.getElementById('btn-enviar-factura');
    const tipo = document.getElementById('factura-tipo').value;
    const cantidadCuotas = tipo === 'CREDITO' ? Number(document.getElementById('factura-cuotas').value) : 1;

    if (tipo === 'CREDITO' && (!Number.isInteger(cantidadCuotas) || cantidadCuotas < 2 || cantidadCuotas > 24)) {
        error.textContent = 'Una factura a crédito debe tener entre 2 y 24 cuotas.';
        error.classList.remove('hidden');
        return;
    }

    const payload = {
        idContrato: Number(document.getElementById('factura-contrato').value),
        periodo: document.getElementById('factura-periodo').value,
        fechaVencimiento: document.getElementById('factura-vencimiento').value || null,
        tipoFactura: tipo,
        cantidadCuotas
    };

    error.classList.add('hidden');
    button.disabled = true;
    try {
        await apiPost('/facturacion/mensual', payload);
        navigateTo('facturas');
    } catch (err) {
        error.textContent = err.message;
        error.classList.remove('hidden');
    } finally {
        button.disabled = false;
    }
}

function abrirLoteFacturasModal() {
    const periodo = document.getElementById('facturas-filter-periodo').value || currentPeriod();
    document.getElementById('lote-periodo').value = periodo;
    document.getElementById('lote-periodo').max = currentPeriod();
    document.getElementById('lote-tipo').value = 'CONTADO';
    document.getElementById('lote-cuotas').value = 3;
    document.getElementById('lote-error').classList.add('hidden');
    actualizarLoteTipoUI();
    document.getElementById('lote-facturas-modal').classList.remove('hidden');
    document.body.classList.add('modal-open');
}

function cerrarLoteFacturasModal() {
    document.getElementById('lote-facturas-modal').classList.add('hidden');
    document.body.classList.remove('modal-open');
}

function actualizarLoteTipoUI() {
    const tipo = document.getElementById('lote-tipo').value;
    const group = document.getElementById('lote-cuotas-group');
    const cuotas = document.getElementById('lote-cuotas');
    const regla = document.getElementById('lote-regla');

    if (tipo === 'CREDITO') {
        group.classList.remove('hidden');
        cuotas.required = true;
        if (Number(cuotas.value) < 2) cuotas.value = 3;
        regla.className = 'payment-rule payment-rule-credit';
        regla.innerHTML = `<strong>A crédito:</strong> cada factura se dividirá en ${Number(cuotas.value) || 3} cuotas mensuales.`;
    } else {
        group.classList.add('hidden');
        cuotas.required = false;
        regla.className = 'payment-rule payment-rule-cash';
        regla.innerHTML = '<strong>Al contado:</strong> cada factura tendrá un único saldo exigible.';
    }
}

async function generarFacturasDelMes() {
    const periodo = document.getElementById('lote-periodo').value || currentPeriod();
    const tipo = document.getElementById('lote-tipo').value;
    const cuotas = tipo === 'CREDITO' ? Number(document.getElementById('lote-cuotas').value) : 1;
    const error = document.getElementById('lote-error');
    const button = document.getElementById('btn-confirmar-lote');

    error.classList.add('hidden');
    if (periodo > currentPeriod()) {
        error.textContent = 'No se pueden generar facturas para períodos futuros.';
        error.classList.remove('hidden');
        return;
    }
    if (tipo === 'CREDITO' && (!Number.isInteger(cuotas) || cuotas < 2 || cuotas > 24)) {
        error.textContent = 'Las facturas a crédito deben tener entre 2 y 24 cuotas.';
        error.classList.remove('hidden');
        return;
    }

    button.disabled = true;
    try {
        const qs = new URLSearchParams({ periodo, tipoFactura: tipo, cantidadCuotas: String(cuotas) });
        const r = await apiPost(`/facturacion/generar-mensuales?${qs.toString()}`, {});
        cerrarLoteFacturasModal();
        alert(`Proceso completado\n\nTipo: ${nombreTipoFactura(tipo)}\nContratos evaluados: ${r.contratosEvaluados}\nFacturas creadas: ${r.facturasCreadas}\nOmitidas: ${r.omitidas?.length || 0}`);
        document.getElementById('facturas-filter-periodo').value = periodo;
        document.getElementById('facturas-filter-tipo').value = tipo;
        await loadFacturas();
    } catch (err) {
        error.textContent = err.message;
        error.classList.remove('hidden');
    } finally {
        button.disabled = false;
    }
}

async function registrarPagoFactura(id) {
    try {
        const f = await apiGet(`/facturacion/${id}`);
        if (f.estado === 'EN_ACUERDO') {
            pagarDesdeAcuerdoFactura(id);
            return;
        }

        facturaPagoActual = f;
        document.getElementById('pago-form').reset();
        document.getElementById('pago-factura-id').value = f.idFactura;
        document.getElementById('pago-factura-numero').textContent = f.numeroECF;
        document.getElementById('pago-inquilino').textContent = f.razonSocial;
        document.getElementById('pago-tipo-factura').textContent = nombreTipoFactura(f.tipoFactura);
        document.getElementById('pago-plan-cuotas').textContent = f.tipoFactura === 'CREDITO' ? `${f.cantidadCuotas || 1} cuotas` : 'Pago único';
        document.getElementById('pago-total').textContent = dinero(f.total);
        document.getElementById('pago-pagado').textContent = dinero(f.montoPagado);
        document.getElementById('pago-pendiente').textContent = dinero(f.montoPendiente);

        const monto = document.getElementById('pago-monto');
        monto.max = Number(f.montoPendiente).toFixed(2);
        monto.value = Number(f.montoPendiente).toFixed(2);
        monto.readOnly = f.tipoFactura === 'CONTADO';
        monto.classList.toggle('input-locked', f.tipoFactura === 'CONTADO');

        const regla = document.getElementById('pago-regla');
        if (f.tipoFactura === 'CONTADO') {
            regla.className = 'payment-rule payment-rule-cash';
            regla.innerHTML = `<strong>Pago único obligatorio.</strong> Para una factura al contado se cobrará exactamente ${dinero(f.montoPendiente)}. El monto está bloqueado para evitar abonos parciales.`;
            document.getElementById('pago-cuotas-section').classList.add('hidden');
        } else {
            regla.className = 'payment-rule payment-rule-credit';
            regla.innerHTML = '<strong>Factura a crédito.</strong> Puede registrar un abono. El sistema lo aplicará primero a la cuota vencida o más antigua con saldo.';
            renderCuotasPagoFactura(f.cuotas || []);
            document.getElementById('pago-cuotas-section').classList.remove('hidden');
            const siguiente = (f.cuotas || []).find(c => Number(c.pendiente) > 0);
            if (siguiente) monto.value = Number(siguiente.pendiente).toFixed(2);
        }

        document.getElementById('pago-fecha').value = todayIso();
        document.getElementById('pago-metodo').value = 'Transferencia';
        document.getElementById('pago-error').classList.add('hidden');
        actualizarResumenPagoFactura();

        document.getElementById('pago-modal-title').textContent = `Registrar pago · ${f.numeroECF}`;
        document.getElementById('pago-modal').classList.remove('hidden');
        document.body.classList.add('modal-open');
        if (!monto.readOnly) setTimeout(() => monto.focus(), 0);
    } catch (error) {
        alert(error.message);
    }
}

function renderCuotasPagoFactura(cuotas) {
    const tbody = document.getElementById('pago-cuotas-body');
    tbody.innerHTML = cuotas.length
        ? cuotas.map(c => `<tr>
            <td>${c.numeroCuota}/${c.totalCuotas}</td>
            <td>${formatearFecha(c.fechaVencimiento)}</td>
            <td>${dinero(c.monto)}</td>
            <td>${dinero(c.pagado)}</td>
            <td><strong>${dinero(c.pendiente)}</strong></td>
            <td><span class="status-badge ${statusClass(c.estado)}">${escapeHtml(c.estado)}</span></td>
        </tr>`).join('')
        : '<tr><td colspan="6" class="empty-state">No hay plan de cuotas disponible.</td></tr>';
}

function actualizarResumenPagoFactura() {
    if (!facturaPagoActual) return;
    const monto = Number(document.getElementById('pago-monto').value || 0);
    const pendiente = Number(facturaPagoActual.montoPendiente || 0);
    const nuevoSaldo = Math.max(0, pendiente - (Number.isFinite(monto) ? monto : 0));
    const preview = document.getElementById('pago-nuevo-saldo');
    preview.innerHTML = `Nuevo saldo después del pago: <strong>${dinero(nuevoSaldo)}</strong>`;

    const invalidoContado = facturaPagoActual.tipoFactura === 'CONTADO' && Math.abs(monto - pendiente) > 0.009;
    preview.classList.toggle('payment-balance-invalid', monto > pendiente || monto <= 0 || invalidoContado);
}

async function guardarPagoFactura(event) {
    event.preventDefault();
    if (!facturaPagoActual) return;

    const error = document.getElementById('pago-error');
    const button = document.getElementById('btn-guardar-pago');
    const monto = Number(document.getElementById('pago-monto').value);
    const pendiente = Number(facturaPagoActual.montoPendiente);
    error.classList.add('hidden');

    if (!Number.isFinite(monto) || monto <= 0) {
        error.textContent = 'Introduzca un monto mayor que cero.';
        error.classList.remove('hidden');
        return;
    }
    if (monto > pendiente + 0.009) {
        error.textContent = `El pago no puede superar el saldo pendiente de ${dinero(pendiente)}.`;
        error.classList.remove('hidden');
        return;
    }
    if (facturaPagoActual.tipoFactura === 'CONTADO' && Math.abs(monto - pendiente) > 0.009) {
        error.textContent = `Una factura al contado debe pagarse completa. El monto requerido es ${dinero(pendiente)}.`;
        error.classList.remove('hidden');
        return;
    }

    const fecha = document.getElementById('pago-fecha').value;
    const payload = {
        monto,
        metodoPago: document.getElementById('pago-metodo').value,
        referencia: document.getElementById('pago-referencia').value.trim() || null,
        fechaPago: fecha ? new Date(`${fecha}T12:00:00`).toISOString() : new Date().toISOString(),
        notas: facturaPagoActual.tipoFactura === 'CONTADO' ? 'Pago total factura al contado' : 'Abono factura a crédito'
    };

    button.disabled = true;
    try {
        await apiPost(`/facturacion/${facturaPagoActual.idFactura}/pagos`, payload);
        cerrarPagoModal();
        await loadFacturas();
    } catch (err) {
        error.textContent = err.message;
        error.classList.remove('hidden');
    } finally {
        button.disabled = false;
    }
}

function cerrarPagoModal() {
    document.getElementById('pago-modal').classList.add('hidden');
    document.body.classList.remove('modal-open');
    facturaPagoActual = null;
}

async function pagarDesdeAcuerdoFactura(idFactura) {
    window.acuerdoFacturaPreseleccionada = idFactura;
    navigateTo('acuerdos-pago');
}

async function anularFactura(id) {
    if (!confirm('¿Anular esta factura? Esta acción crea un crédito en el estado de cuenta.')) return;
    try {
        await apiPut(`/facturacion/${id}/anular`, {});
        await loadFacturas();
    } catch (error) {
        alert(error.message);
    }
}

async function verFactura(id) {
    try {
        const f = await apiGet(`/facturacion/${id}`);
        facturaActualParaImprimir = f;
        document.getElementById('factura-modal-title').textContent = f.numeroECF;
        document.getElementById('factura-modal-content').innerHTML = construirFacturaHtml(f);
        document.getElementById('factura-modal').classList.remove('hidden');
        document.body.classList.add('modal-open');
    } catch (error) {
        alert(error.message);
    }
}

function cerrarFacturaModal() {
    document.getElementById('factura-modal').classList.add('hidden');
    document.body.classList.remove('modal-open');
}

function construirFacturaHtml(f) {
    const detalles = (f.detalles || []).map(d => `<tr><td>${escapeHtml(d.descripcionItem)}</td><td class="text-right">${formatearMonto(d.cantidad)}</td><td class="text-right">${dinero(d.precio)}</td><td class="text-right"><strong>${dinero(d.subtotal)}</strong></td></tr>`).join('');
    const cuotas = f.tipoFactura === 'CREDITO' && (f.cuotas || []).length
        ? `<div class="invoice-installments"><h3>Plan de cuotas</h3><div class="table-responsive"><table class="table invoice-lines"><thead><tr><th>#</th><th>Vencimiento</th><th>Monto</th><th>Pagado</th><th>Pendiente</th><th>Estado</th></tr></thead><tbody>${f.cuotas.map(c => `<tr><td>${c.numeroCuota}/${c.totalCuotas}</td><td>${formatearFecha(c.fechaVencimiento)}</td><td>${dinero(c.monto)}</td><td>${dinero(c.pagado)}</td><td>${dinero(c.pendiente)}</td><td>${escapeHtml(c.estado)}</td></tr>`).join('')}</tbody></table></div></div>`
        : '';

    return `<div class="invoice-brand"><div><div class="invoice-brand-mark">H</div><div><strong>HabitaCont SRL</strong><span>Gestión de arrendamientos</span></div></div><span class="status-badge ${statusClass(f.estado)}">${escapeHtml(f.estado)}</span></div>
        <div class="invoice-meta-grid">
            <div><span>Inquilino</span><strong>${escapeHtml(f.razonSocial)}</strong><small>${escapeHtml(f.rncCedula)}</small></div>
            <div><span>Propiedad</span><strong>${escapeHtml(f.codigoPropiedad || '—')}</strong><small>${escapeHtml(f.direccionPropiedad || '—')}</small></div>
            <div><span>Contrato / período</span><strong>${escapeHtml(f.codigoContrato || 'Factura manual')}</strong><small>${escapeHtml(f.periodoFacturado || f.origenFactura || 'Manual')}</small></div>
            <div><span>Tipo de factura</span><strong>${escapeHtml(nombreTipoFactura(f.tipoFactura))}</strong><small>${f.tipoFactura === 'CREDITO' ? `${f.cantidadCuotas || 1} cuotas` : 'Pago único'}</small></div>
            <div><span>Emisión / vencimiento</span><strong>${formatearFecha(f.fechaEmision)}</strong><small>Vence ${formatearFecha(f.fechaVencimiento)}</small></div>
        </div>
        <div class="table-responsive"><table class="table invoice-lines"><thead><tr><th>Concepto</th><th class="text-right">Cantidad</th><th class="text-right">Precio</th><th class="text-right">Importe</th></tr></thead><tbody>${detalles || '<tr><td colspan="4">Sin detalles</td></tr>'}</tbody></table></div>
        <div class="invoice-totals"><div><span>Subtotal</span><strong>${dinero(f.subtotal)}</strong></div><div><span>ITBIS</span><strong>${dinero(f.itbis)}</strong></div><div class="grand-total"><span>Total</span><strong>${dinero(f.total)}</strong></div><div><span>Pagado</span><strong>${dinero(f.montoPagado)}</strong></div><div><span>Pendiente</span><strong>${dinero(f.montoPendiente)}</strong></div></div>
        ${cuotas}
        <div class="invoice-footnote">Documento académico. La firma DGII mostrada es una simulación para fines del proyecto.</div>`;
}

function imprimirFacturaActual() {
    if (!facturaActualParaImprimir) return;
    const win = window.open('', '_blank', 'width=980,height=760');
    if (!win) return alert('El navegador bloqueó la ventana de impresión.');
    win.document.write(`<!doctype html><html lang="es"><head><meta charset="utf-8"><title>${escapeHtml(facturaActualParaImprimir.numeroECF)}</title><style>body{font-family:Arial,sans-serif;color:#111;padding:32px}.invoice-brand{display:flex;justify-content:space-between;border-bottom:2px solid #111;padding-bottom:16px;margin-bottom:24px}.invoice-brand>div{display:flex;gap:12px}.invoice-brand-mark{width:36px;height:36px;background:#111;color:#fff;display:grid;place-items:center;font-weight:bold}.invoice-brand span,.invoice-meta-grid span,.invoice-meta-grid small{display:block;color:#555}.invoice-meta-grid{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px}.invoice-meta-grid>div{border:1px solid #ddd;padding:12px}table{width:100%;border-collapse:collapse}th,td{border-bottom:1px solid #ddd;padding:9px;text-align:left}.text-right{text-align:right}.invoice-totals{margin:20px 0 0 auto;width:320px}.invoice-totals>div{display:flex;justify-content:space-between;padding:7px}.grand-total{font-size:18px;border-top:2px solid #111;border-bottom:2px solid #111}.status-badge{border:1px solid #333;padding:5px 10px}.invoice-installments{margin-top:30px}.invoice-footnote{margin-top:40px;color:#666;font-size:12px}</style></head><body>${construirFacturaHtml(facturaActualParaImprimir)}<script>window.onload=()=>window.print()<\/script></body></html>`);
    win.document.close();
}
