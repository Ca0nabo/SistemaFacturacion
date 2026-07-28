function initGastos() {
    onView('gastos', initGastosView);
}

function initGastosView() {
    const form = document.getElementById('gasto-form');
    form.reset();

    document.getElementById('gasto-fecha-vencimiento').valueAsDate = new Date();

    cargarFacturasGasto();

    document.getElementById('btn-enviar-gasto').disabled = false;
    document.getElementById('btn-enviar-gasto').textContent = 'Registrar Gasto';
    document.getElementById('gasto-error').classList.add('hidden');
}

async function cargarFacturasGasto() {
    const select = document.getElementById('gasto-factura');
    select.innerHTML = '<option value="">Cargando facturas...</option>';

    try {
        const facturas = await apiGet('/facturacion');
        const disponibles = (facturas || []).filter(f => f.estado === 'EMITIDA');

        select.innerHTML = '<option value="">Seleccione una factura...</option>';
        disponibles.forEach(f => {
            select.innerHTML += `<option value="${f.idFactura}">#${f.idFactura} - ${f.razonSocial ?? ''} (RD$${formatearMonto(f.total ?? 0)})</option>`;
        });

        if (disponibles.length === 0) {
            select.innerHTML = '<option value="">No hay facturas emitidas disponibles</option>';
        }
    } catch (err) {
        select.innerHTML = '<option value="">Error cargando facturas</option>';
    }

    const form = document.getElementById('gasto-form');
    form.onsubmit = async (e) => {
        e.preventDefault();
        const errorDiv = document.getElementById('gasto-error');
        const btnEnviar = document.getElementById('btn-enviar-gasto');
        errorDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Registrando gasto...';

        const formData = new FormData();
        formData.append('idFactura', parseInt(document.getElementById('gasto-factura').value));
        formData.append('monto', parseFloat(document.getElementById('gasto-monto').value));
        formData.append('fechaVencimiento', document.getElementById('gasto-fecha-vencimiento').value);
        formData.append('categoria', document.getElementById('gasto-categoria').value);

        const fileInput = document.getElementById('gasto-evidencia');
        if (fileInput && fileInput.files && fileInput.files[0]) {
            formData.append('evidencia', fileInput.files[0]);
        }

        try {
            await apiPostForm('/movimientos/gasto', formData);
            alert('Gasto registrado exitosamente.');
            navigateTo('gastos');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Registrar Gasto';
        }
    };
}
