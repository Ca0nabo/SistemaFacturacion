function initUnidades() {
    document.getElementById('btn-nueva-unidad').addEventListener('click', () => mostrarFormularioUnidad());
}

async function cargarUnidades(idPropiedad) {
    const tbody = document.getElementById('unidades-body');
    tbody.innerHTML = '<tr><td colspan="6" class="loading">Cargando unidades...</td></tr>';
    try {
        const unidades = await apiGet(`/unidades${queryString({ idPropiedad })}`);
        tbody.innerHTML = unidades.length ? unidades.map(u => `
            <tr>
                <td>${u.idUnidad}</td><td><strong>${escapeHtml(u.codigo)}</strong></td><td>${escapeHtml(u.piso || '—')}</td><td>${u.metrosCuadrados}</td>
                <td><span class="status-badge ${statusClass(u.estado)}">${escapeHtml(u.estado)}</span></td>
                <td class="acciones">${hasPermission('PROPIEDADES.GESTIONAR') ? `<button class="btn btn-sm btn-secondary" onclick="editarUnidad(${u.idUnidad})">Editar</button><select class="inline-select" onchange="cambiarEstadoUnidad(${u.idUnidad}, this.value)"><option value="">Estado...</option><option>Disponible</option><option>Alquilada</option><option value="EnMantenimiento">Mantenimiento</option></select>` : '<span class="muted">Solo lectura</span>'}</td>
            </tr>`).join('') : '<tr><td colspan="6" class="empty-state">Esta propiedad no tiene unidades. Si se alquila completa, no es obligatorio crearlas.</td></tr>';
    } catch (error) { tbody.innerHTML = `<tr><td colspan="6" class="table-error">${escapeHtml(error.message)}</td></tr>`; }
}

let unidadEditandoId = null;
function mostrarFormularioUnidad(unidad = null) {
    cancelarUnidadForm();
    unidadEditandoId = unidad?.idUnidad || null;
    const row = document.createElement('tr');
    row.id = 'unidad-form-row';
    row.innerHTML = `<td>${unidadEditandoId || ''}</td><td><input id="unidad-codigo" value="${escapeHtml(unidad?.codigo || '')}" placeholder="A-101"></td><td><input id="unidad-piso" value="${escapeHtml(unidad?.piso || '')}" placeholder="1"></td><td><input type="number" min="0.01" step="0.01" id="unidad-metros" value="${unidad?.metrosCuadrados || ''}"></td><td>Disponible</td><td><button class="btn btn-sm btn-primary" onclick="guardarUnidad()">Guardar</button> <button class="btn btn-sm btn-secondary" onclick="cancelarUnidadForm()">Cancelar</button></td>`;
    document.getElementById('unidades-body').prepend(row);
}
function cancelarUnidadForm() { document.getElementById('unidad-form-row')?.remove(); unidadEditandoId = null; }

async function guardarUnidad() {
    const idPropiedad = propiedadEditandoId || propiedadUnidadesId;
    const payload = {
        idPropiedad,
        codigo: document.getElementById('unidad-codigo').value.trim(),
        piso: document.getElementById('unidad-piso').value.trim() || null,
        metrosCuadrados: Number(document.getElementById('unidad-metros').value)
    };
    if (!idPropiedad || !payload.codigo || payload.metrosCuadrados <= 0) return alert('Complete código y metros cuadrados.');
    try {
        if (unidadEditandoId) await apiPut(`/unidades/${unidadEditandoId}`, payload);
        else await apiPost('/unidades', payload);
        unidadEditandoId = null;
        await cargarUnidades(idPropiedad);
    } catch (error) { alert(error.message); }
}

async function editarUnidad(id) {
    try { mostrarFormularioUnidad(await apiGet(`/unidades/${id}`)); }
    catch (error) { alert(error.message); }
}
async function cambiarEstadoUnidad(id, estado) {
    if (!estado) return;
    try {
        await apiPatch(`/unidades/${id}/estado`, estado);
        await cargarUnidades(propiedadEditandoId || propiedadUnidadesId);
    } catch (error) { alert(error.message); }
}
