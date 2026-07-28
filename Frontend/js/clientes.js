function initClientes() {
    onView('clientes', () => loadEntidades('clientes', 'Cliente'));
    onView('proveedores', () => loadEntidades('proveedores', 'Proveedor'));
    onView('entidad-form', initEntidadFormView);

    document.getElementById('btn-nuevo-cliente').addEventListener('click', () => abrirFormularioEntidad('Cliente', null));
    document.getElementById('btn-nuevo-proveedor').addEventListener('click', () => abrirFormularioEntidad('Proveedor', null));
    document.getElementById('btn-volver-entidades').addEventListener('click', volverDeEntidadForm);
    document.getElementById('entidad-form').addEventListener('submit', guardarEntidad);
}

async function loadEntidades(viewId, tipo) {
    const tbody = document.getElementById(`${viewId}-body`);
    tbody.innerHTML = '<tr><td colspan="5" class="loading">Cargando...</td></tr>';

    try {
        const entidades = await apiGet('/entidades');
        const filtradas = (entidades || []).filter(e => e.tipo === tipo);

        tbody.innerHTML = filtradas.map(e => {
            const estadoClase = e.activo ? 'status-activo' : 'status-cancelado';
            const estadoTexto = e.activo ? 'Activo' : 'Inactivo';
            return `
            <tr>
                <td>${e.idEntidad ?? ''}</td>
                <td>${e.rncCedula ?? ''}</td>
                <td>${e.razonSocial ?? ''}</td>
                <td><span class="status-badge ${estadoClase}">${estadoTexto}</span></td>
                <td class="acciones">
                    <button class="btn btn-sm btn-secondary" onclick="abrirFormularioEntidad('${tipo}', ${e.idEntidad})">Editar</button>
                    ${e.activo ? `<button class="btn btn-sm btn-danger" onclick="eliminarEntidad(${e.idEntidad}, '${tipo}')">Eliminar</button>` : ''}
                </td>
            </tr>`;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="5" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

async function abrirFormularioEntidad(tipo, id) {
    const title = document.getElementById('entidad-form-title');
    const btnEnviar = document.getElementById('btn-enviar-entidad');
    const errorDiv = document.getElementById('entidad-error');
    errorDiv.classList.add('hidden');

    document.getElementById('entidad-id').value = '';
    document.getElementById('entidad-tipo').value = tipo;
    document.getElementById('entidad-rnc').value = '';
    document.getElementById('entidad-razon').value = '';

    if (id) {
        title.textContent = `Editar ${tipo}`;
        btnEnviar.textContent = 'Actualizar';
        try {
            const entidad = await apiGet(`/entidades/${id}`);
            document.getElementById('entidad-id').value = entidad.idEntidad;
            document.getElementById('entidad-rnc').value = entidad.rncCedula;
            document.getElementById('entidad-razon').value = entidad.razonSocial;
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
            return;
        }
    } else {
        title.textContent = `Nuevo ${tipo}`;
        btnEnviar.textContent = 'Guardar';
    }

    navigateTo('entidad-form');
}

function volverDeEntidadForm() {
    const tipo = document.getElementById('entidad-tipo').value;
    if (tipo === 'Proveedor') {
        navigateTo('proveedores');
    } else {
        navigateTo('clientes');
    }
}

async function guardarEntidad(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('entidad-error');
    const btnEnviar = document.getElementById('btn-enviar-entidad');
    errorDiv.classList.add('hidden');

    const id = document.getElementById('entidad-id').value;
    const tipo = document.getElementById('entidad-tipo').value;
    const rnc = document.getElementById('entidad-rnc').value.trim();
    const razon = document.getElementById('entidad-razon').value.trim();

    if (!rnc || !razon) {
        errorDiv.textContent = 'Complete todos los campos.';
        errorDiv.classList.remove('hidden');
        return;
    }

    btnEnviar.disabled = true;
    btnEnviar.textContent = 'Guardando...';

    try {
        if (id) {
            await apiPut(`/entidades/${id}`, { tipo, rncCedula: rnc, razonSocial: razon });
        } else {
            await apiPost('/entidades', { tipo, rncCedula: rnc, razonSocial: razon });
        }
        navigateTo(tipo === 'Proveedor' ? 'proveedores' : 'clientes');
    } catch (err) {
        errorDiv.textContent = err.message;
        errorDiv.classList.remove('hidden');
    } finally {
        btnEnviar.disabled = false;
        btnEnviar.textContent = id ? 'Actualizar' : 'Guardar';
    }
}

async function eliminarEntidad(id, tipo) {
    if (!confirm(`¿Está seguro de eliminar este ${tipo}?`)) return;
    try {
        await apiDelete(`/entidades/${id}`);
        navigateTo(tipo === 'Proveedor' ? 'proveedores' : 'clientes');
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

function initEntidadFormView() {
    document.getElementById('entidad-form').reset();
}
