let movimientosCuentaCache = [];

function initMovimientos() {
    onView('movimientos', initMovimientosView);
    document.getElementById('btn-filtrar-movimientos').addEventListener('click', cargarMovimientos);
    document.getElementById('btn-limpiar-movimientos').addEventListener('click', limpiarFiltrosMovimientos);
    document.getElementById('btn-exportar-movimientos').addEventListener('click', exportarMovimientos);
}

async function initMovimientosView() {
    await cargarFiltrosMovimientos();
    await cargarMovimientos();
}

async function cargarFiltrosMovimientos() {
    try {
        const [clientes, propietarios, propiedades, contratos] = await Promise.all([
            apiGet('/entidades?tipo=Cliente'), apiGet('/entidades?tipo=Propietario'), apiGet('/propiedades'), apiGet('/contratos')
        ]);
        const fill = (id, first, data, value, label) => {
            const select = document.getElementById(id); const current = select.value;
            select.innerHTML = `<option value="">${first}</option>` + data.map(x => `<option value="${value(x)}">${escapeHtml(label(x))}</option>`).join('');
            if (current) select.value = current;
        };
        fill('mov-filter-cliente', 'Todos los inquilinos', clientes, x => x.idEntidad, x => x.razonSocial);
        fill('mov-filter-propietario', 'Todos los propietarios', propietarios, x => x.idEntidad, x => x.razonSocial);
        fill('mov-filter-propiedad', 'Todas las propiedades', propiedades, x => x.idPropiedad, x => `${x.codigo} - ${x.direccion}`);
        fill('mov-filter-contrato', 'Todos los contratos', contratos, x => x.idContrato, x => `${x.codigoContrato} - ${x.razonSocial}`);
    } catch (error) { console.error(error); }
}

function paramsMovimientos() {
    return {
        idEntidad: document.getElementById('mov-filter-cliente').value,
        idPropiedad: document.getElementById('mov-filter-propiedad').value,
        idPropietario: document.getElementById('mov-filter-propietario').value,
        idContrato: document.getElementById('mov-filter-contrato').value,
        desde: document.getElementById('mov-filter-desde').value,
        hasta: document.getElementById('mov-filter-hasta').value
    };
}

async function cargarMovimientos() {
    const body = document.getElementById('movimientos-body');
    const pendingBody = document.getElementById('movimientos-pendientes-body');
    body.innerHTML = '<tr><td colspan="10" class="loading">Cargando movimientos...</td></tr>';
    pendingBody.innerHTML = '<tr><td colspan="7" class="loading">Cargando cuentas...</td></tr>';
    const params = paramsMovimientos();
    try {
        const [movimientos, resumen, pendientes] = await Promise.all([
            apiGet(`/movimientos/cuenta${queryString(params)}`),
            apiGet(`/movimientos/resumen${queryString(params)}`),
            apiGet(`/movimientos${queryString({ tipo: 'CxC', idEntidad: params.idEntidad, idPropiedad: params.idPropiedad, idPropietario: params.idPropietario, idContrato: params.idContrato, soloPendientes: true })}`)
        ]);
        movimientosCuentaCache = movimientos;
        document.getElementById('mov-resumen-debito').textContent = dinero(resumen.montoFacturado);
        document.getElementById('mov-resumen-credito').textContent = dinero(resumen.montoPagado);
        document.getElementById('mov-resumen-saldo').textContent = dinero(resumen.montoPendiente);
        body.innerHTML = movimientos.length ? movimientos.map(m => `<tr>
            <td>${formatearFechaHora(m.fecha)}</td><td>${escapeHtml(m.entidad)}</td><td>${escapeHtml(m.propiedad || '—')}</td><td>${escapeHtml(m.codigoContrato || '—')}</td><td>${escapeHtml(m.numeroFactura || '—')}</td>
            <td><strong>${escapeHtml(m.tipoMovimiento)}</strong><br><span class="muted">${escapeHtml(m.concepto)}</span></td><td>${escapeHtml(m.referencia || '—')}</td>
            <td class="money debit">${m.debito ? dinero(m.debito) : '—'}</td><td class="money credit">${m.credito ? dinero(m.credito) : '—'}</td><td class="money"><strong>${dinero(m.saldo)}</strong></td>
        </tr>`).join('') : '<tr><td colspan="10" class="empty-state">No hay movimientos para los filtros elegidos.</td></tr>';
        pendingBody.innerHTML = pendientes.length ? pendientes.map(m => `<tr><td>${escapeHtml(m.numeroFactura)}</td><td>${escapeHtml(m.entidad)}</td><td>${escapeHtml(m.direccionPropiedad || '—')}</td><td>${formatearFecha(m.fechaVencimiento)}</td><td>${dinero(m.montoOriginal)}</td><td><strong>${dinero(m.montoPendiente)}</strong></td><td>${m.estadoFactura === 'EN_ACUERDO' ? '<span class="muted">En acuerdo de pago</span>' : (hasPermission('FACTURAS.PAGAR') ? `<button class="btn btn-sm btn-success" onclick="pagarMovimientoCompleto(${m.idMovimiento})">Pagar completo</button>` : '<span class="muted">Solo lectura</span>')}</td></tr>`).join('') : '<tr><td colspan="7" class="empty-state">No hay cuentas pendientes.</td></tr>';
    } catch (error) {
        body.innerHTML = `<tr><td colspan="10" class="table-error">${escapeHtml(error.message)}</td></tr>`;
        pendingBody.innerHTML = '<tr><td colspan="7" class="empty-state">No disponible.</td></tr>';
    }
}

function limpiarFiltrosMovimientos() {
    ['mov-filter-cliente','mov-filter-propiedad','mov-filter-propietario','mov-filter-contrato','mov-filter-desde','mov-filter-hasta'].forEach(id => document.getElementById(id).value = '');
    cargarMovimientos();
}
async function pagarMovimientoCompleto(id) {
    if (!confirm('¿Registrar el pago completo de esta cuenta por cobrar?')) return;
    try { await apiPut(`/movimientos/${id}/pagar`, {}); await cargarMovimientos(); }
    catch (error) { alert(error.message); }
}
function exportarMovimientos() {
    if (!movimientosCuentaCache.length) return alert('No hay movimientos para exportar.');
    const rows = [['Fecha','Inquilino','Propiedad','Contrato','Factura','Tipo','Concepto','Referencia','Débito','Crédito','Balance']];
    movimientosCuentaCache.forEach(m => rows.push([formatearFechaHora(m.fecha),m.entidad,m.propiedad,m.codigoContrato,m.numeroFactura,m.tipoMovimiento,m.concepto,m.referencia,m.debito,m.credito,m.saldo]));
    downloadCsv(`movimientos-${todayIso()}.csv`, rows);
}
