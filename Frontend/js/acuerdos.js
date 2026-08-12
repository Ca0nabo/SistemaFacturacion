let acuerdosCache = [];
let contratosAcuerdoCache = [];
let facturasAcuerdoCache = [];
let acuerdoActualVisualizado = null;
let acuerdoPagoCuotaActual = null;

function initAcuerdos() {
    onView('acuerdos-pago', loadAcuerdos);
    onView('acuerdo-form', initAcuerdoFormView);
    document.getElementById('btn-nuevo-acuerdo').addEventListener('click', () => navigateTo('acuerdo-form'));
    document.getElementById('btn-volver-acuerdos').addEventListener('click', () => navigateTo('acuerdos-pago'));
    document.getElementById('acuerdo-form').addEventListener('submit', guardarAcuerdo);
    document.getElementById('acuerdo-contrato').addEventListener('change', cargarFacturasAcuerdo);
    document.getElementById('acuerdo-factura').addEventListener('change', completarAcuerdoDesdeFactura);
    document.getElementById('acuerdo-original').addEventListener('input', () => { document.getElementById('acuerdo-acordado').value = document.getElementById('acuerdo-original').value; actualizarPreviewAcuerdo(); });
    document.getElementById('acuerdo-cuotas').addEventListener('input', actualizarPreviewAcuerdo);
    document.getElementById('acuerdo-pago-form').addEventListener('submit', guardarPagoCuotaAcuerdo);
    document.getElementById('btn-cerrar-acuerdo-pago-modal').addEventListener('click', cerrarPagoCuotaAcuerdoModal);
    document.getElementById('btn-cancelar-acuerdo-pago').addEventListener('click', cerrarPagoCuotaAcuerdoModal);
    document.querySelector('[data-close-acuerdo-pago-modal]').addEventListener('click', cerrarPagoCuotaAcuerdoModal);
}

async function loadAcuerdos() {
    const tbody = document.getElementById('acuerdos-body');
    document.getElementById('acuerdo-cuotas-card').classList.add('hidden');
    tbody.innerHTML = '<tr><td colspan="10" class="loading">Cargando acuerdos...</td></tr>';
    try {
        acuerdosCache = await apiGet('/acuerdos-pago');
        if (!acuerdosCache.length) {
            tbody.innerHTML = '<tr><td colspan="10" class="empty-state">No hay acuerdos de pago registrados.</td></tr>';
            return;
        }
        tbody.innerHTML = acuerdosCache.map(a => `<tr>
            <td><strong>ACP-${String(a.idAcuerdo).padStart(6,'0')}</strong></td><td>${escapeHtml(a.codigoContrato)}<br><span class="muted">${escapeHtml(a.inquilino)}</span></td><td>${escapeHtml(a.numeroFacturaOrigen || 'Sin factura')}</td>
            <td>${dinero(a.montoOriginal)}</td><td>${dinero(a.montoAcordado)}</td><td>${dinero(a.montoPagado)}</td><td><strong>${dinero(a.saldoPendiente)}</strong></td><td>${a.cantidadCuotas} × ${dinero(a.montoCuota)}</td>
            <td><span class="status-badge ${statusClass(a.estado)}">${escapeHtml(a.estado)}</span></td><td class="acciones"><button class="btn btn-sm btn-secondary" onclick="verCuotasAcuerdo(${a.idAcuerdo})">Ver cuotas</button>${a.estado === 'Activo' ? `<button class="btn btn-sm btn-danger" onclick="cancelarAcuerdo(${a.idAcuerdo})">Cancelar</button>` : ''}</td>
        </tr>`).join('');

        if (window.acuerdoFacturaPreseleccionada) {
            const objetivo = acuerdosCache.find(a => a.idFacturaOrigen === Number(window.acuerdoFacturaPreseleccionada) && a.estado === 'Activo');
            window.acuerdoFacturaPreseleccionada = null;
            if (objetivo) setTimeout(() => verCuotasAcuerdo(objetivo.idAcuerdo), 0);
        }
    } catch (error) { tbody.innerHTML = `<tr><td colspan="10" class="table-error">${escapeHtml(error.message)}</td></tr>`; }
}

async function initAcuerdoFormView() {
    document.getElementById('acuerdo-form').reset();
    document.getElementById('acuerdo-fecha-inicio').value = todayIso();
    document.getElementById('acuerdo-dia-pago').value = 5;
    document.getElementById('acuerdo-cuotas').value = 3;
    document.getElementById('acuerdo-error').classList.add('hidden');
    document.getElementById('acuerdo-factura').innerHTML = '<option value="">Seleccione primero un contrato</option>';
    try {
        contratosAcuerdoCache = await apiGet('/contratos');
        document.getElementById('acuerdo-contrato').innerHTML = '<option value="">Seleccione un contrato...</option>' + contratosAcuerdoCache.map(c => `<option value="${c.idContrato}">${escapeHtml(c.codigoContrato)} · ${escapeHtml(c.razonSocial)} · ${escapeHtml(c.codigoPropiedad || '')}</option>`).join('');
    } catch (error) {
        const div = document.getElementById('acuerdo-error'); div.textContent = error.message; div.classList.remove('hidden');
    }
    actualizarPreviewAcuerdo();
}

async function cargarFacturasAcuerdo() {
    const idContrato = Number(document.getElementById('acuerdo-contrato').value);
    const select = document.getElementById('acuerdo-factura');
    select.innerHTML = '<option value="">Seleccione una factura pendiente...</option>';
    if (!idContrato) return;
    try {
        const todas = await apiGet(`/facturacion${queryString({ idContrato })}`);
        facturasAcuerdoCache = todas.filter(f => f.tipoFactura === 'CREDITO' && f.montoPendiente > 0 && !['ANULADA','EN_ACUERDO'].includes(f.estado));
        select.innerHTML += facturasAcuerdoCache.map(f => `<option value="${f.idFactura}">${escapeHtml(f.numeroECF)} · A crédito · ${escapeHtml(f.periodoFacturado || f.origenFactura || '')} · pendiente ${dinero(f.montoPendiente)}</option>`).join('');
        const contrato = contratosAcuerdoCache.find(c => c.idContrato === idContrato);
        if (contrato) document.getElementById('acuerdo-dia-pago').value = contrato.diaPago;
    } catch (error) { alert(error.message); }
}
function completarAcuerdoDesdeFactura() {
    const f = facturasAcuerdoCache.find(x => x.idFactura === Number(document.getElementById('acuerdo-factura').value));
    if (f) {
        document.getElementById('acuerdo-original').value = f.montoPendiente;
        document.getElementById('acuerdo-acordado').value = f.montoPendiente;
    }
    actualizarPreviewAcuerdo();
}
function actualizarPreviewAcuerdo() {
    const monto = Number(document.getElementById('acuerdo-acordado').value || 0);
    const cuotas = Number(document.getElementById('acuerdo-cuotas').value || 0);
    document.querySelector('#acuerdo-preview strong').textContent = cuotas > 0 ? dinero(monto / cuotas) : dinero(0);
}

async function guardarAcuerdo(event) {
    event.preventDefault();
    const error = document.getElementById('acuerdo-error');
    const button = document.getElementById('btn-enviar-acuerdo');
    const payload = {
        idContrato: Number(document.getElementById('acuerdo-contrato').value),
        idFacturaOrigen: Number(document.getElementById('acuerdo-factura').value),
        montoOriginal: Number(document.getElementById('acuerdo-original').value),
        montoAcordado: Number(document.getElementById('acuerdo-acordado').value),
        cantidadCuotas: Number(document.getElementById('acuerdo-cuotas').value),
        fechaInicio: document.getElementById('acuerdo-fecha-inicio').value,
        diaPago: Number(document.getElementById('acuerdo-dia-pago').value),
        observaciones: document.getElementById('acuerdo-observaciones').value.trim() || null
    };
    error.classList.add('hidden'); button.disabled = true;
    try { await apiPost('/acuerdos-pago', payload); navigateTo('acuerdos-pago'); }
    catch (err) { error.textContent = err.message; error.classList.remove('hidden'); }
    finally { button.disabled = false; }
}

async function verCuotasAcuerdo(id) {
    acuerdoActualVisualizado = id;
    try {
        const a = await apiGet(`/acuerdos-pago/${id}`);
        const card = document.getElementById('acuerdo-cuotas-card');
        document.getElementById('acuerdo-cuotas-title').textContent = `Cuotas de ACP-${String(id).padStart(6,'0')} · ${a.inquilino}`;
        document.getElementById('acuerdo-cuotas-body').innerHTML = a.cuotas.map(c => `<tr><td>${c.numeroCuota}</td><td>${formatearFecha(c.fechaVencimiento)}</td><td>${dinero(c.monto)}</td><td>${dinero(c.montoPagado)}</td><td><strong>${dinero(c.saldoPendiente)}</strong></td><td><span class="status-badge ${statusClass(c.estado)}">${escapeHtml(c.estado)}</span></td><td>${c.saldoPendiente > 0 && a.estado === 'Activo' ? `<button class="btn btn-sm btn-success" onclick="pagarCuotaAcuerdo(${c.idCuotaAcuerdo}, ${c.saldoPendiente}, ${c.numeroCuota})">Pagar</button>` : '—'}</td></tr>`).join('');
        card.classList.remove('hidden');
        card.scrollIntoView({ behavior: 'smooth' });
    } catch (error) { alert(error.message); }
}
function pagarCuotaAcuerdo(idCuota, saldo, numeroCuota) {
    acuerdoPagoCuotaActual = { idCuota, saldo: Number(saldo), numeroCuota };
    document.getElementById('acuerdo-pago-form').reset();
    document.getElementById('acuerdo-pago-cuota-id').value = idCuota;
    document.getElementById('acuerdo-pago-cuota-numero').textContent = `Cuota ${numeroCuota}`;
    document.getElementById('acuerdo-pago-saldo').textContent = dinero(saldo);
    document.getElementById('acuerdo-pago-monto').value = Number(saldo).toFixed(2);
    document.getElementById('acuerdo-pago-monto').max = Number(saldo).toFixed(2);
    document.getElementById('acuerdo-pago-metodo').value = 'Transferencia';
    document.getElementById('acuerdo-pago-error').classList.add('hidden');
    document.getElementById('acuerdo-pago-modal').classList.remove('hidden');
    document.body.classList.add('modal-open');
}

function cerrarPagoCuotaAcuerdoModal() {
    document.getElementById('acuerdo-pago-modal').classList.add('hidden');
    document.body.classList.remove('modal-open');
    acuerdoPagoCuotaActual = null;
}

async function guardarPagoCuotaAcuerdo(event) {
    event.preventDefault();
    if (!acuerdoPagoCuotaActual) return;

    const monto = Number(document.getElementById('acuerdo-pago-monto').value);
    const saldo = acuerdoPagoCuotaActual.saldo;
    const error = document.getElementById('acuerdo-pago-error');
    const button = document.getElementById('btn-guardar-acuerdo-pago');
    error.classList.add('hidden');

    if (!Number.isFinite(monto) || monto <= 0 || monto > saldo + 0.009) {
        error.textContent = `El monto debe ser mayor que cero y no superar ${dinero(saldo)}.`;
        error.classList.remove('hidden');
        return;
    }

    button.disabled = true;
    try {
        await apiPost(`/acuerdos-pago/cuotas/${acuerdoPagoCuotaActual.idCuota}/pagar`, {
            monto,
            metodoPago: document.getElementById('acuerdo-pago-metodo').value,
            referencia: document.getElementById('acuerdo-pago-referencia').value.trim() || null
        });
        const idAcuerdo = acuerdoActualVisualizado;
        cerrarPagoCuotaAcuerdoModal();
        await loadAcuerdos();
        if (idAcuerdo) await verCuotasAcuerdo(idAcuerdo);
    } catch (err) {
        error.textContent = err.message;
        error.classList.remove('hidden');
    } finally {
        button.disabled = false;
    }
}

async function cancelarAcuerdo(id) {
    if (!confirm('¿Cancelar este acuerdo de pago?')) return;
    try { await apiDelete(`/acuerdos-pago/${id}`); await loadAcuerdos(); }
    catch (error) { alert(error.message); }
}
