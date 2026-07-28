function initContratos() {
    onView('contratos', loadContratos);

    document.getElementById('btn-nuevo-contrato').addEventListener('click', () => {
        contratoEditandoId = null;
        navigateTo('contrato-form');
    });

    document.getElementById('btn-volver-contratos').addEventListener('click', () => {
        contratoEditandoId = null;
        navigateTo('contratos');
    });

    onView('contrato-form', initContratoFormView);

    document.getElementById('contrato-propiedad').addEventListener('change', async () => {
        const propId = document.getElementById('contrato-propiedad').value;
        const selectUnidad = document.getElementById('contrato-unidad');
        selectUnidad.innerHTML = '<option value="">Sin unidad específica</option>';
        if (propId) {
            try {
                const unidades = await apiGet(`/unidades?idPropiedad=${propId}&estado=Disponible`);
                unidades.forEach(u => {
                    selectUnidad.innerHTML += `<option value="${u.idUnidad}">${u.codigo} (${u.metrosCuadrados}m²)</option>`;
                });
            } catch {}
        }
    });

    initContratoForm();
}

async function loadContratos() {
    const tbody = document.getElementById('contratos-body');
    tbody.innerHTML = '<tr><td colspan="10" class="loading">Cargando contratos...</td></tr>';

    try {
        const contratos = await apiGet('/contratos');
        tbody.innerHTML = (contratos || []).map(c => {
            const estado = (c.estado || '').toLowerCase();
            return `
            <tr>
                <td>${c.idContrato ?? ''}</td>
                <td>${c.razonSocial ?? ''}</td>
                <td>${c.rncCedula ?? ''}</td>
                <td>${c.direccionPropiedad ?? ''}${c.codigoUnidad ? ' - ' + c.codigoUnidad : ''}</td>
                <td>${c.tipoContrato ?? ''}</td>
                <td>RD$${formatearMonto(c.monto ?? 0)}</td>
                <td>${formatearFecha(c.fechaInicio)}</td>
                <td>${formatearFecha(c.fechaVencimiento)}</td>
                <td><span class="status-badge status-${estado}">${c.estado}</span></td>
                <td class="acciones">
                    <button class="btn btn-sm btn-secondary" onclick="editarContrato(${c.idContrato})">Editar</button>
                    <select class="estado-contrato" data-id="${c.idContrato}" onchange="cambiarEstadoContrato(this)">
                        <option value="">Estado</option>
                        <option value="Pendiente">Pendiente</option>
                        <option value="Activo">Activo</option>
                        <option value="Vencido">Vencido</option>
                        <option value="Cancelado">Cancelado</option>
                    </select>
                </td>
            </tr>`;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="10" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

async function cambiarEstadoContrato(select) {
    const estado = select.value;
    if (!estado) return;
    const id = select.dataset.id;
    try {
        await apiPatch(`/contratos/${id}/estado`, { nuevoEstado: estado });
        loadContratos();
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

let contratoEditandoId = null;

async function editarContrato(id) {
    try {
        const c = await apiGet(`/contratos/${id}`);
        contratoEditandoId = id;
        document.getElementById('contrato-form').reset();
        document.getElementById('contrato-cliente').value = c.idEntidad;
        document.getElementById('contrato-tipo').value = c.tipoContrato;
        document.getElementById('contrato-monto').value = c.monto;
        document.getElementById('contrato-mantenimiento').value = c.montoMantenimiento || 0;
        document.getElementById('contrato-deposito').value = c.deposito || 0;
        document.getElementById('contrato-dia-pago').value = c.diaPago || 5;
        document.getElementById('contrato-fecha-inicio').value = c.fechaInicio;
        document.getElementById('contrato-fecha-vencimiento').value = c.fechaVencimiento;
        document.getElementById('contrato-condiciones').value = c.condiciones;
        await cargarPropiedadesContrato(c.idPropiedad);
        if (c.idUnidad) {
            document.getElementById('contrato-unidad').value = c.idUnidad;
        }
        document.getElementById('btn-enviar-contrato').textContent = 'Actualizar Contrato';
        document.querySelector('#view-contrato-form .page-header h1').textContent = 'Editar Contrato';
        navigateTo('contrato-form');
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

function initContratoFormView() {
    if (!contratoEditandoId) {
        document.getElementById('contrato-form').reset();
        document.getElementById('contrato-fecha-inicio').valueAsDate = new Date();
        document.getElementById('btn-enviar-contrato').textContent = 'Registrar Contrato';
        document.querySelector('#view-contrato-form .page-header h1').textContent = 'Nuevo Contrato';
    }
    cargarClientesContrato();
    cargarPropiedadesContrato(null);
}

function initContratoForm() {
    const form = document.getElementById('contrato-form');
    const btnEnviar = document.getElementById('btn-enviar-contrato');
    const errorDiv = document.getElementById('contrato-error');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Registrando contrato...';

        const payload = {
            idEntidad: parseInt(document.getElementById('contrato-cliente').value),
            idPropiedad: parseInt(document.getElementById('contrato-propiedad').value),
            idUnidad: parseInt(document.getElementById('contrato-unidad').value) || null,
            tipoContrato: document.getElementById('contrato-tipo').value,
            condiciones: document.getElementById('contrato-condiciones').value.trim(),
            fechaInicio: document.getElementById('contrato-fecha-inicio').value,
            fechaVencimiento: document.getElementById('contrato-fecha-vencimiento').value,
            monto: parseFloat(document.getElementById('contrato-monto').value),
            montoMantenimiento: parseFloat(document.getElementById('contrato-mantenimiento').value) || null,
            deposito: parseFloat(document.getElementById('contrato-deposito').value) || null,
            diaPago: parseInt(document.getElementById('contrato-dia-pago').value) || 5
        };

        if (!payload.idEntidad || !payload.idPropiedad) {
            errorDiv.textContent = 'Seleccione un cliente y una propiedad.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Registrar Contrato';
            return;
        }

        try {
            if (contratoEditandoId) {
                await apiPut(`/contratos/${contratoEditandoId}`, payload);
                contratoEditandoId = null;
            } else {
                await apiPost('/contratos', payload);
            }
            navigateTo('contratos');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = contratoEditandoId ? 'Actualizar Contrato' : 'Registrar Contrato';
        }
    });
}

async function cargarClientesContrato() {
    const select = document.getElementById('contrato-cliente');
    select.innerHTML = '<option value="">Cargando clientes...</option>';

    try {
        const entidades = await apiGet('/entidades');
        const clientes = (entidades || []).filter(e => (e.tipo === 'Cliente' || e.tipo === 'Inquilino') && e.activo);
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

async function cargarPropiedadesContrato(selectedId) {
    const select = document.getElementById('contrato-propiedad');
    select.innerHTML = '<option value="">Cargando propiedades...</option>';

    try {
        const propiedades = await apiGet('/propiedades');
        select.innerHTML = '<option value="">Seleccione una propiedad...</option>';
        propiedades.forEach(p => {
            const sel = p.idPropiedad === selectedId ? 'selected' : '';
            select.innerHTML += `<option value="${p.idPropiedad}" ${sel}>${p.direccion} (${p.tipoPropiedad})</option>`;
        });
        if (propiedades.length === 0) {
            select.innerHTML = '<option value="">No hay propiedades disponibles</option>';
        }
        if (selectedId) {
            const event = new Event('change');
            select.dispatchEvent(event);
        }
    } catch (err) {
        select.innerHTML = '<option value="">Error cargando propiedades</option>';
    }
}

function formatearFecha(fechaStr) {
    if (!fechaStr) return '';
    const d = new Date(fechaStr + 'T00:00:00');
    if (isNaN(d.getTime())) return fechaStr;
    return d.toLocaleDateString('es-DO');
}
