function initUnidades() {
    document.getElementById('btn-nueva-unidad').addEventListener('click', () => {
        mostrarFormularioUnidad();
    });

    initUnidadForm();
}

async function cargarUnidades(idPropiedad) {
    const tbody = document.getElementById('unidades-body');
    const unidadesCard = document.getElementById('unidades-card');
    unidadesCard.style.display = 'block';
    tbody.innerHTML = '<tr><td colspan="6" class="loading">Cargando unidades...</td></tr>';

    try {
        const unidades = await apiGet(`/unidades?idPropiedad=${idPropiedad}`);
        tbody.innerHTML = (unidades || []).map(u => {
            const est = (u.estado || '').toLowerCase();
            return `
            <tr>
                <td>${u.idUnidad ?? ''}</td>
                <td>${u.codigo ?? ''}</td>
                <td>${u.piso ?? ''}</td>
                <td>${(u.metrosCuadrados ?? 0).toLocaleString('es-DO')}</td>
                <td><span class="status-badge status-${est}">${u.estado}</span></td>
                <td class="acciones">
                    <button class="btn btn-sm btn-secondary" onclick="editarUnidad(${u.idUnidad})">Editar</button>
                    <select class="estado-unidad" data-id="${u.idUnidad}" onchange="cambiarEstadoUnidad(this)">
                        <option value="">Estado</option>
                        <option value="Disponible">Disponible</option>
                        <option value="Alquilada">Alquilada</option>
                        <option value="EnMantenimiento">Mantenimiento</option>
                    </select>
                </td>
            </tr>`;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

let unidadEditandoId = null;

function mostrarFormularioUnidad() {
    unidadEditandoId = null;
    const row = document.createElement('tr');
    row.id = 'unidad-form-row';
    row.innerHTML = `
        <td></td>
        <td><input type="text" id="unidad-codigo" placeholder="Ej: A-101" style="width:100%" required></td>
        <td><input type="text" id="unidad-piso" placeholder="Ej: 1" style="width:100%"></td>
        <td><input type="number" id="unidad-metros" min="0.01" step="0.01" style="width:100%" required></td>
        <td colspan="2">
            <button class="btn btn-sm btn-primary" onclick="guardarUnidad()">Guardar</button>
            <button class="btn btn-sm btn-secondary" onclick="cancelarUnidadForm()">Cancelar</button>
        </td>
    `;
    document.getElementById('unidades-body').prepend(row);
}

function cancelarUnidadForm() {
    const row = document.getElementById('unidad-form-row');
    if (row) row.remove();
    unidadEditandoId = null;
}

async function guardarUnidad() {
    const idPropiedad = propiedadEditandoId || propiedadUnidadesId;
    const codigo = document.getElementById('unidad-codigo').value.trim();
    const piso = document.getElementById('unidad-piso').value.trim() || null;
    const metros = parseFloat(document.getElementById('unidad-metros').value);

    if (!codigo || !metros) {
        alert('Complete los campos obligatorios.');
        return;
    }

    const payload = { idPropiedad, codigo, piso, metrosCuadrados: metros };

    try {
        if (unidadEditandoId) {
            await apiPut(`/unidades/${unidadEditandoId}`, payload);
            unidadEditandoId = null;
        } else {
            await apiPost('/unidades', payload);
        }
        cargarUnidades(idPropiedad);
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

async function editarUnidad(id) {
    try {
        const u = await apiGet(`/unidades/${id}`);
        unidadEditandoId = id;
        mostrarFormularioUnidad();
        document.getElementById('unidad-codigo').value = u.codigo;
        document.getElementById('unidad-piso').value = u.piso || '';
        document.getElementById('unidad-metros').value = u.metrosCuadrados;
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}

function initUnidadForm() {
    document.addEventListener('click', (e) => {
        if (e.target.classList.contains('estado-unidad')) return;
    });
}

async function cambiarEstadoUnidad(select) {
    const estado = select.value;
    if (!estado) return;
    const id = select.dataset.id;
    try {
        await apiPatch(`/unidades/${id}/estado`, JSON.stringify(estado));
        const idPropiedad = propiedadEditandoId || propiedadUnidadesId;
        if (idPropiedad) cargarUnidades(idPropiedad);
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}
