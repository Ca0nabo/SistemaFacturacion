function initGastos() {
    onView('gastos', initGastosView);
    document.getElementById('gasto-form').addEventListener('submit', guardarGasto);
}

async function initGastosView() {
    document.getElementById('gasto-form').reset();
    document.getElementById('gasto-fecha-vencimiento').value = todayIso();
    document.getElementById('gasto-error').classList.add('hidden');
    const select = document.getElementById('gasto-factura');
    select.innerHTML = '<option value="">Cargando facturas...</option>';
    try {
        const facturas = await apiGet('/facturacion');
        select.innerHTML = '<option value="">Seleccione una factura de referencia...</option>' + facturas.filter(f => f.estado !== 'ANULADA').map(f => `<option value="${f.idFactura}">${escapeHtml(f.numeroECF)} · ${escapeHtml(f.razonSocial)} · ${dinero(f.total)}</option>`).join('');
    } catch (error) { select.innerHTML = '<option value="">No se pudieron cargar facturas</option>'; }
}

async function guardarGasto(event) {
    event.preventDefault();
    const error = document.getElementById('gasto-error');
    const button = document.getElementById('btn-enviar-gasto');
    const form = new FormData();
    form.append('idFactura', document.getElementById('gasto-factura').value);
    form.append('tipo', 'CxP');
    form.append('montoOriginal', document.getElementById('gasto-monto').value);
    form.append('fechaVencimiento', document.getElementById('gasto-fecha-vencimiento').value);
    form.append('categoriaGasto', document.getElementById('gasto-categoria').value);
    const file = document.getElementById('gasto-evidencia').files[0];
    if (file) form.append('archivo', file);
    error.classList.add('hidden'); button.disabled = true;
    try {
        await apiPostForm('/movimientos/gasto', form);
        alert('Gasto registrado correctamente.');
        document.getElementById('gasto-form').reset();
        document.getElementById('gasto-fecha-vencimiento').value = todayIso();
    } catch (err) { error.textContent = err.message; error.classList.remove('hidden'); }
    finally { button.disabled = false; }
}
