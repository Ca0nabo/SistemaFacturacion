function initFacturas() {
    onView('facturas', loadFacturas);

    document.getElementById('btn-nueva-factura').addEventListener('click', () => {
        navigateTo('factura-form');
    });

    document.getElementById('btn-volver-facturas').addEventListener('click', () => {
        navigateTo('facturas');
    });

    onView('factura-form', initFacturaFormView);

    initFacturaForm();
}

async function loadFacturas() {
    const tbody = document.getElementById('facturas-body');
    tbody.innerHTML = '<tr><td colspan="10" class="loading">Cargando facturas...</td></tr>';

    try {
        const facturas = await apiGet('/facturacion');
        tbody.innerHTML = (facturas || []).map(f => {
            const estado = (f.estado || '').toLowerCase();
            const puedeAnular = f.estado !== 'ANULADA' && f.estado !== 'PAGADA';
            return `
            <tr>
                <td>${f.idFactura ?? ''}</td>
                <td><strong>${f.numeroECF ?? ''}</strong></td>
                <td>${f.razonSocial ?? ''}</td>
                <td>${f.rncCedula ?? ''}</td>
                <td>${new Date(f.fechaEmision).toLocaleDateString('es-DO')}</td>
                <td>RD$${formatearMonto(f.subtotal ?? 0)}</td>
                <td>RD$${formatearMonto(f.itbis ?? 0)}</td>
                <td>RD$${formatearMonto(f.total ?? 0)}</td>
                <td><span class="status-badge status-${estado}">${f.estado}</span></td>
                <td class="acciones">
                    ${puedeAnular ? `<button class="btn btn-sm btn-danger" onclick="anularFactura(${f.idFactura})">Anular</button>` : ''}
                </td>
            </tr>`;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="10" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

function initFacturaFormView() {
    const form = document.getElementById('factura-form');
    form.reset();

    const tbody = document.getElementById('detalle-body');
    tbody.innerHTML = `
        <tr class="detalle-row">
            <td><input type="text" class="item-desc" placeholder="Descripción del ítem" required></td>
            <td><input type="number" class="item-cant" min="0.01" step="0.01" value="1" required></td>
            <td><input type="number" class="item-precio" min="0.01" step="0.01" value="0" required></td>
            <td><span class="item-subtotal">RD$0.00</span></td>
            <td><button type="button" class="btn-remove-row" title="Eliminar">×</button></td>
        </tr>`;

    document.getElementById('factura-fecha').valueAsDate = new Date();
    document.getElementById('factura-cuotas').value = 1;
    recalcularDetalle();
    cargarClientes();
}

function initFacturaForm() {
    const form = document.getElementById('factura-form');
    const btnAdd = document.getElementById('btn-add-row');
    const btnEnviar = document.getElementById('btn-enviar-factura');
    const errorDiv = document.getElementById('factura-error');

    btnAdd.addEventListener('click', () => agregarFilaDetalle());

    document.addEventListener('input', (e) => {
        if (e.target.classList.contains('item-cant') || e.target.classList.contains('item-precio')) {
            recalcularDetalle();
        }
    });

    document.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-remove-row')) {
            const rows = document.querySelectorAll('.detalle-row');
            if (rows.length > 1) {
                e.target.closest('tr').remove();
                recalcularDetalle();
            }
        }
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Emitiendo factura...';

        const rows = document.querySelectorAll('.detalle-row');
        const detalles = [];
        let valid = true;

        rows.forEach(row => {
            const descEl = row.querySelector('.item-desc');
            const cantEl = row.querySelector('.item-cant');
            const precioEl = row.querySelector('.item-precio');
            if (!descEl || !cantEl || !precioEl) {
                valid = false;
                return;
            }
            const desc = descEl.value.trim();
            const cant = parseFloat(cantEl.value);
            const precio = parseFloat(precioEl.value);

            if (!desc || !cant || !precio) {
                valid = false;
                return;
            }

            detalles.push({
                descripcionItem: desc,
                cantidad: cant,
                precio: precio
            });
        });

        if (!valid || detalles.length === 0) {
            errorDiv.textContent = 'Complete todos los detalles de la factura.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Emitir Factura Electrónica';
            return;
        }

        const idEntidad = parseInt(document.getElementById('factura-cliente').value);
        const cuotas = parseInt(document.getElementById('factura-cuotas').value) || 1;

        if (!idEntidad) {
            errorDiv.textContent = 'Seleccione un cliente.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Emitir Factura Electrónica';
            return;
        }

        try {
            const result = await apiPost('/facturacion', {
                idEntidad: idEntidad,
                detalles: detalles,
                cuotas: cuotas
            });
            alert(`Factura emitida exitosamente.\n\nNúmero e-CF: ${result.numeroECF}\nTotal: RD$${formatearMonto(result.total ?? 0)}\nFirma DGII: ${result.firmaDGII ?? ''}`);
            navigateTo('facturas');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Emitir Factura Electrónica';
        }
    });
}

async function cargarClientes() {
    const select = document.getElementById('factura-cliente');
    select.innerHTML = '<option value="">Cargando clientes...</option>';

    try {
        const entidades = await apiGet('/entidades');
        const clientes = (entidades || []).filter(e => e.tipo === 'Cliente' && e.activo);

        select.innerHTML = '<option value="">Seleccione un cliente...</option>';
        clientes.forEach(c => {
            select.innerHTML += `<option value="${c.idEntidad}">${c.razonSocial} (${c.rncCedula})</option>`;
        });

        if (clientes.length === 0) {
            select.innerHTML = '<option value="">No hay clientes activos disponibles</option>';
        }
    } catch (err) {
        select.innerHTML = '<option value="">Error cargando clientes</option>';
    }
}

function agregarFilaDetalle() {
    const tbody = document.getElementById('detalle-body');
    const row = document.createElement('tr');
    row.className = 'detalle-row';
    row.innerHTML = `
        <td><input type="text" class="item-desc" placeholder="Descripción del ítem" required></td>
        <td><input type="number" class="item-cant" min="0.01" step="0.01" value="1" required></td>
        <td><input type="number" class="item-precio" min="0.01" step="0.01" value="0" required></td>
        <td><span class="item-subtotal">RD$0.00</span></td>
        <td><button type="button" class="btn-remove-row" title="Eliminar">×</button></td>
    `;
    tbody.appendChild(row);
    recalcularDetalle();
}

async function anularFactura(id) {
    if (!confirm('¿Está seguro de anular esta factura?')) return;
    try {
        await apiPut(`/facturacion/${id}/anular`);
        loadFacturas();
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

function recalcularDetalle() {
    const rows = document.querySelectorAll('.detalle-row');
    let subtotal = 0;

    rows.forEach(row => {
        const cant = parseFloat(row.querySelector('.item-cant').value) || 0;
        const precio = parseFloat(row.querySelector('.item-precio').value) || 0;
        const sub = cant * precio;
        subtotal += sub;
        row.querySelector('.item-subtotal').textContent = `RD$${formatearMonto(sub)}`;
    });

    const itbis = subtotal * 0.18;
    const total = subtotal + itbis;

    document.getElementById('factura-subtotal').textContent = `RD$${formatearMonto(subtotal)}`;
    document.getElementById('factura-itbis').textContent = `RD$${formatearMonto(itbis)}`;
    document.getElementById('factura-total').textContent = `RD$${formatearMonto(total)}`;
}
