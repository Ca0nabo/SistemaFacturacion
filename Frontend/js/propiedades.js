function initPropiedades() {
    onView('propiedades', loadPropiedades);

    document.getElementById('btn-nueva-propiedad').addEventListener('click', () => {
        propiedadEditandoId = null;
        navigateTo('propiedad-form');
    });

    document.getElementById('btn-volver-propiedades').addEventListener('click', () => {
        propiedadEditandoId = null;
        navigateTo('propiedades');
    });

    onView('propiedad-form', initPropiedadFormView);
    initPropiedadForm();
    initPropiedadFilters();
}

async function loadPropiedades() {
    const tbody = document.getElementById('propiedades-body');
    tbody.innerHTML = '<tr><td colspan="9" class="loading">Cargando propiedades...</td></tr>';

    const tipo = document.getElementById('filter-tipo-propiedad').value;
    const estado = document.getElementById('filter-estado-propiedad').value;
    let url = '/propiedades';
    const params = [];
    if (tipo) params.push(`tipo=${encodeURIComponent(tipo)}`);
    if (estado) params.push(`estado=${encodeURIComponent(estado)}`);
    if (params.length) url += '?' + params.join('&');

    try {
        const propiedades = await apiGet(url);
        tbody.innerHTML = (propiedades || []).map(p => {
            const est = (p.estado || '').toLowerCase();
            return `
            <tr>
                <td>${p.idPropiedad ?? ''}</td>
                <td>${p.tipoPropiedad ?? ''}</td>
                <td>${p.direccion ?? ''}</td>
                <td>${p.sector ?? ''}</td>
                <td>${p.razonSocialPropietario ?? ''}</td>
                <td>${(p.metrosCuadrados ?? 0).toLocaleString('es-DO')}</td>
                <td>${p.cantidadUnidades ?? 0} / ${p.cantidadUnidadesOcupadas ?? 0} ocup.</td>
                <td><span class="status-badge status-${est}">${p.estado}</span></td>
                <td class="acciones">
                    <button class="btn btn-sm btn-secondary" onclick="editarPropiedad(${p.idPropiedad})">Editar</button>
                    <button class="btn btn-sm btn-secondary" onclick="verUnidades(${p.idPropiedad})">Unidades</button>
                </td>
            </tr>`;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="9" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

function initPropiedadFilters() {
    document.getElementById('filter-tipo-propiedad').addEventListener('change', loadPropiedades);
    document.getElementById('filter-estado-propiedad').addEventListener('change', loadPropiedades);
}

let propiedadEditandoId = null;

async function editarPropiedad(id) {
    try {
        const p = await apiGet(`/propiedades/${id}`);
        propiedadEditandoId = id;
        document.getElementById('propiedad-form').reset();
        document.getElementById('propiedad-id').value = p.idPropiedad;
        document.getElementById('propiedad-propietario').value = p.idEntidad;
        document.getElementById('propiedad-tipo').value = p.tipoPropiedad;
        document.getElementById('propiedad-direccion').value = p.direccion;
        document.getElementById('propiedad-sector').value = p.sector || '';
        document.getElementById('propiedad-ciudad').value = p.ciudad || '';
        document.getElementById('propiedad-metros').value = p.metrosCuadrados;
        document.getElementById('propiedad-habitaciones').value = p.cantidadHabitaciones || '';
        document.getElementById('propiedad-banos').value = p.cantidadBanos || '';
        document.getElementById('propiedad-parqueo').checked = p.tieneParqueo;
        document.getElementById('btn-enviar-propiedad').textContent = 'Actualizar Propiedad';
        document.querySelector('#view-propiedad-form .page-header h1').textContent = 'Editar Propiedad';
        navigateTo('propiedad-form');
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

function verUnidades(id) {
    propiedadUnidadesId = id;
    navigateTo('propiedad-form');
}

let propiedadUnidadesId = null;

function initPropiedadFormView() {
    cargarPropietarios();
    const unidadesCard = document.getElementById('unidades-card');
    const id = propiedadEditandoId || propiedadUnidadesId;

    if (id) {
        unidadesCard.style.display = 'block';
        cargarUnidades(id);
    } else {
        unidadesCard.style.display = 'none';
        document.getElementById('propiedad-form').reset();
        document.getElementById('btn-enviar-propiedad').textContent = 'Guardar Propiedad';
        document.querySelector('#view-propiedad-form .page-header h1').textContent = 'Nueva Propiedad';
    }
}

function initPropiedadForm() {
    const form = document.getElementById('propiedad-form');
    const btnEnviar = document.getElementById('btn-enviar-propiedad');
    const errorDiv = document.getElementById('propiedad-error');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Guardando...';

        const payload = {
            idEntidad: parseInt(document.getElementById('propiedad-propietario').value),
            tipoPropiedad: document.getElementById('propiedad-tipo').value,
            direccion: document.getElementById('propiedad-direccion').value.trim(),
            sector: document.getElementById('propiedad-sector').value.trim() || null,
            ciudad: document.getElementById('propiedad-ciudad').value.trim() || null,
            metrosCuadrados: parseFloat(document.getElementById('propiedad-metros').value),
            cantidadHabitaciones: parseInt(document.getElementById('propiedad-habitaciones').value) || null,
            cantidadBanos: parseInt(document.getElementById('propiedad-banos').value) || null,
            tieneParqueo: document.getElementById('propiedad-parqueo').checked
        };

        if (!payload.idEntidad || !payload.tipoPropiedad || !payload.direccion) {
            errorDiv.textContent = 'Complete los campos obligatorios.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = propiedadEditandoId ? 'Actualizar Propiedad' : 'Guardar Propiedad';
            return;
        }

        try {
            if (propiedadEditandoId) {
                await apiPut(`/propiedades/${propiedadEditandoId}`, payload);
                propiedadEditandoId = null;
            } else {
                await apiPost('/propiedades', payload);
            }
            navigateTo('propiedades');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = propiedadEditandoId ? 'Actualizar Propiedad' : 'Guardar Propiedad';
        }
    });
}

async function cargarPropietarios() {
    const select = document.getElementById('propiedad-propietario');
    select.innerHTML = '<option value="">Cargando propietarios...</option>';

    try {
        const entidades = await apiGet('/entidades');
        const props = (entidades || []).filter(e => (e.tipo === 'Propietario' || e.tipo === 'Cliente') && e.activo);
        select.innerHTML = '<option value="">Seleccione un propietario...</option>';
        props.forEach(e => {
            select.innerHTML += `<option value="${e.idEntidad}">${e.razonSocial} (${e.rncCedula})</option>`;
        });
        if (props.length === 0) {
            select.innerHTML = '<option value="">No hay propietarios disponibles</option>';
        }
    } catch (err) {
        select.innerHTML = '<option value="">Error cargando propietarios</option>';
    }
}
