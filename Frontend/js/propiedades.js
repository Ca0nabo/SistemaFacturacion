let propiedadEditandoId = null;
let propiedadUnidadesId = null;

function initPropiedades() {
    onView('propiedades', loadPropiedades);
    onView('propiedad-form', initPropiedadFormView);
    document.getElementById('btn-nueva-propiedad').addEventListener('click', () => {
        propiedadEditandoId = null;
        propiedadUnidadesId = null;
        navigateTo('propiedad-form');
    });
    document.getElementById('btn-volver-propiedades').addEventListener('click', () => navigateTo('propiedades'));
    document.getElementById('filter-tipo-propiedad').addEventListener('change', loadPropiedades);
    document.getElementById('filter-estado-propiedad').addEventListener('change', loadPropiedades);
    document.getElementById('propiedad-form').addEventListener('submit', guardarPropiedad);
}

async function loadPropiedades() {
    const tbody = document.getElementById('propiedades-body');
    tbody.innerHTML = '<tr><td colspan="8" class="loading">Cargando propiedades...</td></tr>';
    const qs = queryString({
        tipo: document.getElementById('filter-tipo-propiedad').value,
        estado: document.getElementById('filter-estado-propiedad').value,
        incluirInactivas: true
    });
    try {
        const propiedades = await apiGet(`/propiedades${qs}`);
        if (!propiedades.length) {
            tbody.innerHTML = '<tr><td colspan="8" class="empty-state">No hay propiedades registradas.</td></tr>';
            return;
        }
        tbody.innerHTML = propiedades.map(p => `
            <tr>
                <td><strong>${escapeHtml(p.codigo)}</strong></td>
                <td><strong>${escapeHtml(p.tipoPropiedad)}</strong><br><span class="muted">${escapeHtml(p.direccion)}</span></td>
                <td>${escapeHtml(p.razonSocialPropietario)}</td>
                <td>${dinero(p.canonMensualSugerido)}</td>
                <td>${dinero(p.mantenimientoMensualSugerido)}</td>
                <td>${p.cantidadUnidades} (${p.cantidadUnidadesOcupadas} ocupadas)</td>
                <td><span class="status-badge ${statusClass(p.estado)}">${escapeHtml(p.estado)}</span></td>
                <td class="acciones">${hasPermission('PROPIEDADES.GESTIONAR') ? `<button class="btn btn-sm btn-secondary" onclick="editarPropiedad(${p.idPropiedad})">Editar</button>` : ''}<button class="btn btn-sm btn-secondary" onclick="verUnidades(${p.idPropiedad})">Unidades</button></td>
            </tr>`).join('');
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="8" class="table-error">${escapeHtml(error.message)}</td></tr>`;
    }
}

async function initPropiedadFormView() {
    await cargarPropietarios();
    const form = document.getElementById('propiedad-form');
    const card = document.getElementById('unidades-card');
    const heading = document.querySelector('#view-propiedad-form .page-header h1');

    if (!propiedadEditandoId && !propiedadUnidadesId) {
        form.reset();
        document.getElementById('propiedad-id').value = '';
        document.getElementById('propiedad-mantenimiento').value = '0';
        heading.textContent = 'Nueva propiedad';
        document.getElementById('btn-enviar-propiedad').textContent = 'Guardar propiedad';
        card.style.display = 'none';
        return;
    }

    const id = propiedadEditandoId || propiedadUnidadesId;
    try {
        const p = await apiGet(`/propiedades/${id}`);
        propiedadEditandoId = id;
        document.getElementById('propiedad-id').value = p.idPropiedad;
        document.getElementById('propiedad-propietario').value = p.idEntidad;
        document.getElementById('propiedad-codigo').value = p.codigo;
        document.getElementById('propiedad-tipo').value = p.tipoPropiedad;
        document.getElementById('propiedad-direccion').value = p.direccion;
        document.getElementById('propiedad-sector').value = p.sector || '';
        document.getElementById('propiedad-ciudad').value = p.ciudad || '';
        document.getElementById('propiedad-metros').value = p.metrosCuadrados;
        document.getElementById('propiedad-habitaciones').value = p.cantidadHabitaciones ?? '';
        document.getElementById('propiedad-banos').value = p.cantidadBanos ?? '';
        document.getElementById('propiedad-canon').value = p.canonMensualSugerido;
        document.getElementById('propiedad-mantenimiento').value = p.mantenimientoMensualSugerido;
        document.getElementById('propiedad-parqueo').checked = p.tieneParqueo;
        heading.textContent = `Editar propiedad ${p.codigo}`;
        document.getElementById('btn-enviar-propiedad').textContent = 'Actualizar propiedad';
        card.style.display = 'block';
        await cargarUnidades(id);
    } catch (error) { alert(error.message); }
}

async function cargarPropietarios() {
    const select = document.getElementById('propiedad-propietario');
    const actual = select.value;
    try {
        const propietarios = await apiGet('/entidades?tipo=Propietario');
        select.innerHTML = '<option value="">Seleccione un propietario...</option>' + propietarios.map(p => `<option value="${p.idEntidad}">${escapeHtml(p.razonSocial)} (${escapeHtml(p.rncCedula)})</option>`).join('');
        if (actual) select.value = actual;
    } catch { select.innerHTML = '<option value="">No se pudieron cargar propietarios</option>'; }
}

async function guardarPropiedad(event) {
    event.preventDefault();
    const error = document.getElementById('propiedad-error');
    const button = document.getElementById('btn-enviar-propiedad');
    const id = document.getElementById('propiedad-id').value;
    const payload = {
        idEntidad: Number(document.getElementById('propiedad-propietario').value),
        codigo: document.getElementById('propiedad-codigo').value.trim(),
        tipoPropiedad: document.getElementById('propiedad-tipo').value,
        direccion: document.getElementById('propiedad-direccion').value.trim(),
        sector: document.getElementById('propiedad-sector').value.trim() || null,
        ciudad: document.getElementById('propiedad-ciudad').value.trim() || null,
        metrosCuadrados: Number(document.getElementById('propiedad-metros').value),
        cantidadHabitaciones: Number(document.getElementById('propiedad-habitaciones').value) || null,
        cantidadBanos: Number(document.getElementById('propiedad-banos').value) || null,
        tieneParqueo: document.getElementById('propiedad-parqueo').checked,
        canonMensualSugerido: Number(document.getElementById('propiedad-canon').value),
        mantenimientoMensualSugerido: Number(document.getElementById('propiedad-mantenimiento').value || 0)
    };
    error.classList.add('hidden');
    button.disabled = true;
    try {
        if (id) await apiPut(`/propiedades/${id}`, payload);
        else await apiPost('/propiedades', payload);
        propiedadEditandoId = null;
        propiedadUnidadesId = null;
        navigateTo('propiedades');
    } catch (err) {
        error.textContent = err.message;
        error.classList.remove('hidden');
    } finally { button.disabled = false; }
}

async function editarPropiedad(id) {
    propiedadEditandoId = id;
    propiedadUnidadesId = id;
    navigateTo('propiedad-form');
}
function verUnidades(id) { propiedadEditandoId = id; propiedadUnidadesId = id; navigateTo('propiedad-form'); }
