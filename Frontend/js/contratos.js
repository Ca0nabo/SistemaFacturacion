let contratoEditandoId = null;
let contratosCache = [];
let propiedadesContratoCache = [];

function initContratos() {
    onView('contratos', loadContratos);
    onView('contrato-form', initContratoFormView);
    document.getElementById('btn-nuevo-contrato').addEventListener('click', () => { contratoEditandoId = null; navigateTo('contrato-form'); });
    document.getElementById('btn-volver-contratos').addEventListener('click', () => navigateTo('contratos'));
    document.getElementById('contrato-form').addEventListener('submit', guardarContrato);
    document.getElementById('contrato-propiedad').addEventListener('change', onPropiedadContratoChange);
    ['contrato-monto', 'contrato-mantenimiento'].forEach(id => document.getElementById(id).addEventListener('input', actualizarTotalContrato));
}

async function loadContratos() {
    const tbody = document.getElementById('contratos-body');
    tbody.innerHTML = '<tr><td colspan="10" class="loading">Cargando contratos...</td></tr>';
    try {
        contratosCache = await apiGet('/contratos');
        if (!contratosCache.length) {
            tbody.innerHTML = '<tr><td colspan="10" class="empty-state">No hay contratos. Primero cree una propiedad y un inquilino.</td></tr>';
            return;
        }
        tbody.innerHTML = contratosCache.map(c => `
            <tr>
                <td><strong>${escapeHtml(c.codigoContrato)}</strong><br><span class="muted">${escapeHtml(c.tipoContrato)}</span></td>
                <td>${escapeHtml(c.razonSocial)}<br><span class="muted">${escapeHtml(c.rncCedula)}</span></td>
                <td><strong>${escapeHtml(c.codigoPropiedad || '')}</strong> ${c.codigoUnidad ? `/ ${escapeHtml(c.codigoUnidad)}` : ''}<br><span class="muted">${escapeHtml(c.direccionPropiedad || '')}</span></td>
                <td>${dinero(c.montoAlquilerMensual)}</td><td>${dinero(c.montoMantenimiento)}</td><td><strong>${dinero(c.totalMensual)}</strong></td>
                <td>${formatearFecha(c.fechaInicio)}<br><span class="muted">a ${formatearFecha(c.fechaVencimiento)}</span></td><td>${c.diaPago}</td>
                <td><span class="status-badge ${statusClass(c.estado)}">${escapeHtml(c.estado)}</span></td>
                <td class="acciones">${hasPermission('CONTRATOS.GESTIONAR') ? `<button class="btn btn-sm btn-secondary" onclick="editarContrato(${c.idContrato})">Editar</button>` : ''}${hasPermission('FACTURAS.CREAR') ? `<button class="btn btn-sm btn-primary" onclick="facturarContrato(${c.idContrato})" ${c.estado !== 'Activo' ? 'disabled' : ''}>Facturar</button>` : ''}${hasPermission('DEPOSITOS.GESTIONAR') ? `<button class="btn btn-sm btn-secondary" onclick="abrirDepositoDesdeContrato(${c.idContrato})">Depósito</button>` : ''}${!hasPermission('CONTRATOS.GESTIONAR') && !hasPermission('FACTURAS.CREAR') && !hasPermission('DEPOSITOS.GESTIONAR') ? '<span class="muted">Solo lectura</span>' : ''}</td>
            </tr>`).join('');
    } catch (error) { tbody.innerHTML = `<tr><td colspan="10" class="table-error">${escapeHtml(error.message)}</td></tr>`; }
}

async function initContratoFormView() {
    const form = document.getElementById('contrato-form');
    const heading = document.querySelector('#view-contrato-form .page-header h1');
    const error = document.getElementById('contrato-error');
    error.classList.add('hidden');
    await Promise.all([cargarInquilinosContrato(), cargarPropiedadesContrato()]);

    if (!contratoEditandoId) {
        form.reset();
        document.getElementById('contrato-dia-pago').value = 5;
        document.getElementById('contrato-mantenimiento').value = 0;
        document.getElementById('contrato-deposito').value = 0;
        document.getElementById('contrato-fecha-inicio').value = todayIso();
        const end = new Date(); end.setFullYear(end.getFullYear() + 1);
        document.getElementById('contrato-fecha-vencimiento').value = end.toISOString().slice(0, 10);
        heading.textContent = 'Nuevo contrato de arrendamiento';
        document.getElementById('btn-enviar-contrato').textContent = 'Guardar contrato';
        actualizarTotalContrato();
        return;
    }

    try {
        const c = await apiGet(`/contratos/${contratoEditandoId}`);
        document.getElementById('contrato-cliente').value = c.idEntidad;
        document.getElementById('contrato-propiedad').value = c.idPropiedad;
        await cargarUnidadesContrato(c.idPropiedad, c.idUnidad);
        document.getElementById('contrato-tipo').value = c.tipoContrato;
        document.getElementById('contrato-condiciones').value = c.condiciones;
        document.getElementById('contrato-fecha-inicio').value = c.fechaInicio;
        document.getElementById('contrato-fecha-vencimiento').value = c.fechaVencimiento;
        document.getElementById('contrato-monto').value = c.montoAlquilerMensual;
        document.getElementById('contrato-mantenimiento').value = c.montoMantenimiento;
        document.getElementById('contrato-deposito').value = c.depositoRequerido;
        document.getElementById('contrato-dia-pago').value = c.diaPago;
        document.getElementById('contrato-itbis').checked = c.aplicaITBIS;
        heading.textContent = `Editar ${c.codigoContrato}`;
        document.getElementById('btn-enviar-contrato').textContent = 'Actualizar contrato';
        actualizarTotalContrato();
    } catch (error) { alert(error.message); }
}

async function cargarInquilinosContrato() {
    const select = document.getElementById('contrato-cliente');
    const actual = select.value;
    try {
        const clientes = await apiGet('/entidades?tipo=Cliente');
        select.innerHTML = '<option value="">Seleccione un inquilino...</option>' + clientes.map(c => `<option value="${c.idEntidad}">${escapeHtml(c.razonSocial)} (${escapeHtml(c.rncCedula)})</option>`).join('');
        if (actual) select.value = actual;
    } catch { select.innerHTML = '<option value="">Error cargando inquilinos</option>'; }
}
async function cargarPropiedadesContrato() {
    const select = document.getElementById('contrato-propiedad');
    const actual = select.value;
    try {
        propiedadesContratoCache = await apiGet('/propiedades');
        select.innerHTML = '<option value="">Seleccione una propiedad...</option>' + propiedadesContratoCache.map(p => `<option value="${p.idPropiedad}">${escapeHtml(p.codigo)} - ${escapeHtml(p.direccion)} (${escapeHtml(p.estado)})</option>`).join('');
        if (actual) select.value = actual;
    } catch { select.innerHTML = '<option value="">Error cargando propiedades</option>'; }
}

async function onPropiedadContratoChange() {
    const id = Number(document.getElementById('contrato-propiedad').value);
    await cargarUnidadesContrato(id);
    const p = propiedadesContratoCache.find(x => x.idPropiedad === id);
    if (p && !contratoEditandoId) {
        document.getElementById('contrato-monto').value = p.canonMensualSugerido || 0;
        document.getElementById('contrato-mantenimiento').value = p.mantenimientoMensualSugerido || 0;
        document.getElementById('contrato-deposito').value = p.canonMensualSugerido || 0;
        actualizarTotalContrato();
    }
}
async function cargarUnidadesContrato(idPropiedad, selectedId = null) {
    const select = document.getElementById('contrato-unidad');
    select.innerHTML = '<option value="">Propiedad completa</option>';
    if (!idPropiedad) return;
    try {
        const unidades = await apiGet(`/unidades${queryString({ idPropiedad })}`);
        unidades.filter(u => u.activo !== false).forEach(u => {
            select.insertAdjacentHTML('beforeend', `<option value="${u.idUnidad}">${escapeHtml(u.codigo)} - ${escapeHtml(u.estado)}</option>`);
        });
        if (selectedId) select.value = selectedId;
    } catch { /* property can be rented without units */ }
}
function actualizarTotalContrato() {
    const total = Number(document.getElementById('contrato-monto').value || 0) + Number(document.getElementById('contrato-mantenimiento').value || 0);
    document.querySelector('#contrato-total-preview strong').textContent = dinero(total);
}

async function guardarContrato(event) {
    event.preventDefault();
    const error = document.getElementById('contrato-error');
    const button = document.getElementById('btn-enviar-contrato');
    const payload = {
        idEntidad: Number(document.getElementById('contrato-cliente').value),
        idPropiedad: Number(document.getElementById('contrato-propiedad').value),
        idUnidad: Number(document.getElementById('contrato-unidad').value) || null,
        tipoContrato: document.getElementById('contrato-tipo').value,
        condiciones: document.getElementById('contrato-condiciones').value.trim(),
        fechaInicio: document.getElementById('contrato-fecha-inicio').value,
        fechaVencimiento: document.getElementById('contrato-fecha-vencimiento').value,
        montoAlquilerMensual: Number(document.getElementById('contrato-monto').value),
        montoMantenimiento: Number(document.getElementById('contrato-mantenimiento').value || 0),
        depositoRequerido: Number(document.getElementById('contrato-deposito').value || 0),
        diaPago: Number(document.getElementById('contrato-dia-pago').value),
        aplicaITBIS: document.getElementById('contrato-itbis').checked
    };
    error.classList.add('hidden');
    button.disabled = true;
    try {
        if (contratoEditandoId) await apiPut(`/contratos/${contratoEditandoId}`, payload);
        else await apiPost('/contratos', payload);
        contratoEditandoId = null;
        navigateTo('contratos');
    } catch (err) { error.textContent = err.message; error.classList.remove('hidden'); }
    finally { button.disabled = false; }
}
async function editarContrato(id) { contratoEditandoId = id; navigateTo('contrato-form'); }
function facturarContrato(id) { window.facturaContratoPreseleccionado = id; navigateTo('factura-form'); }
