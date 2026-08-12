let rolEditandoId = null;
let rolesCatalogoPermisos = [];

function mostrarRolesFeedback(message) {
    const feedback = document.getElementById('roles-feedback');
    if (!feedback) return;
    feedback.textContent = message;
    feedback.classList.remove('hidden');
    window.setTimeout(() => feedback.classList.add('hidden'), 4500);
}

function initRoles() {
    onView('roles', loadRoles);
    onView('rol-form', prepararRolForm);

    document.getElementById('btn-nuevo-rol')?.addEventListener('click', () => {
        rolEditandoId = null;
        navigateTo('rol-form');
    });
    document.getElementById('btn-volver-roles')?.addEventListener('click', () => {
        rolEditandoId = null;
        navigateTo('roles');
    });
    document.getElementById('btn-marcar-todos')?.addEventListener('click', () => {
        document.querySelectorAll('#permisos-matrix input[type="checkbox"]:not(:disabled)').forEach(c => c.checked = true);
        actualizarConteoPermisos();
    });
    document.getElementById('btn-limpiar-permisos')?.addEventListener('click', () => {
        document.querySelectorAll('#permisos-matrix input[type="checkbox"]:not(:disabled)').forEach(c => c.checked = false);
        actualizarConteoPermisos();
    });
    document.getElementById('rol-form')?.addEventListener('submit', guardarRol);
}

async function loadRoles() {
    const tbody = document.getElementById('roles-body');
    tbody.innerHTML = '<tr><td colspan="5" class="loading">Cargando roles...</td></tr>';
    try {
        const [roles, catalogo] = await Promise.all([apiGet('/roles'), apiGet('/roles/permisos')]);
        rolesCatalogoPermisos = catalogo || [];
        const totalPermisos = rolesCatalogoPermisos.reduce((acc, g) => acc + (g.permisos?.length || 0), 0);
        const totalUsuarios = roles.reduce((acc, r) => acc + Number(r.usuarios || 0), 0);
        document.getElementById('roles-summary').innerHTML = `
            <div class="security-summary-card"><span>Roles definidos</span><strong>${roles.length}</strong></div>
            <div class="security-summary-card"><span>Usuarios asignados</span><strong>${totalUsuarios}</strong></div>
            <div class="security-summary-card"><span>Permisos disponibles</span><strong>${totalPermisos}</strong></div>`;

        tbody.innerHTML = roles.length ? roles.map(r => {
            const count = Array.isArray(r.permisos) ? r.permisos.length : 0;
            const type = r.protegido ? 'Protegido' : (r.esSistema ? 'Base' : 'Personalizado');
            const actions = r.protegido
                ? '<span class="system-role-lock">Acceso total</span>'
                : `${hasPermission('ROLES.GESTIONAR') ? `<button class="btn btn-sm btn-secondary" onclick="editarRol(${r.idRol})">Editar</button>` : ''}${(!r.esSistema && hasPermission('ROLES.GESTIONAR')) ? `<button class="btn btn-sm btn-danger" onclick="eliminarRol(${r.idRol})">Eliminar</button>` : ''}`;
            return `<tr>
                <td><div class="role-name-cell"><span class="role-shield">${escapeHtml(r.nombre).slice(0,1).toUpperCase()}</span><strong>${escapeHtml(r.nombre)}</strong></div></td>
                <td>${r.usuarios}</td>
                <td><span class="permission-count-badge">${r.protegido ? 'Todos' : `${count} permisos`}</span></td>
                <td><span class="role-type-badge ${r.protegido ? 'protected' : ''}">${type}</span></td>
                <td class="acciones">${actions || '—'}</td>
            </tr>`;
        }).join('') : '<tr><td colspan="5" class="empty-state">No hay roles configurados.</td></tr>';
        applyPermissionVisibility(document.getElementById('view-roles'));
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="5" class="table-error">${escapeHtml(error.message)}</td></tr>`;
    }
}

async function prepararRolForm() {
    const form = document.getElementById('rol-form');
    const error = document.getElementById('rol-error');
    error.classList.add('hidden');
    form.reset();
    document.getElementById('btn-enviar-rol').classList.remove('hidden');
    document.getElementById('btn-marcar-todos').disabled = false;
    document.getElementById('btn-limpiar-permisos').disabled = false;
    document.getElementById('rol-nombre').disabled = false;

    try {
        if (!rolesCatalogoPermisos.length) rolesCatalogoPermisos = await apiGet('/roles/permisos');
        renderPermissionMatrix([]);
    } catch (err) {
        error.textContent = `No se pudo cargar el catálogo de permisos: ${err.message}`;
        error.classList.remove('hidden');
        document.getElementById('btn-enviar-rol').disabled = true;
        return;
    }

    if (!rolEditandoId) {
        document.getElementById('rol-form-title').textContent = 'Nuevo rol';
        document.getElementById('btn-enviar-rol').textContent = 'Crear rol';
        document.getElementById('rol-nombre').disabled = false;
        return;
    }

    const role = await apiGet(`/roles/${rolEditandoId}`);
    document.getElementById('rol-form-title').textContent = `Editar ${role.nombre}`;
    document.getElementById('rol-nombre').value = role.nombre;
    document.getElementById('rol-nombre').disabled = Boolean(role.protegido);
    renderPermissionMatrix(role.permisos || [], Boolean(role.protegido));
    document.getElementById('btn-enviar-rol').classList.toggle('hidden', Boolean(role.protegido));
    document.getElementById('btn-marcar-todos').disabled = Boolean(role.protegido);
    document.getElementById('btn-limpiar-permisos').disabled = Boolean(role.protegido);
}

function renderPermissionMatrix(selected = [], locked = false) {
    const selectedSet = new Set(selected);
    const host = document.getElementById('permisos-matrix');
    host.innerHTML = rolesCatalogoPermisos.map(group => `
        <section class="permission-group">
            <div class="permission-group-header"><h3>${escapeHtml(group.modulo)}</h3><span>${group.permisos.length} permisos</span></div>
            <div class="permission-options">
                ${group.permisos.map(p => `
                    <label class="permission-option">
                        <input type="checkbox" value="${escapeHtml(p.key)}" ${selectedSet.has(p.key) ? 'checked' : ''} ${locked ? 'disabled' : ''}>
                        <span class="permission-check"></span>
                        <span class="permission-copy"><strong>${escapeHtml(p.accion)}</strong><small>${escapeHtml(p.descripcion)}</small></span>
                    </label>`).join('')}
            </div>
        </section>`).join('');
    host.querySelectorAll('input[type="checkbox"]').forEach(c => c.addEventListener('change', actualizarConteoPermisos));
    actualizarConteoPermisos();
}

function actualizarConteoPermisos() {
    const count = document.querySelectorAll('#permisos-matrix input[type="checkbox"]:checked').length;
    document.getElementById('rol-permission-count').textContent = `${count} seleccionado${count === 1 ? '' : 's'}`;
}

async function editarRol(id) {
    rolEditandoId = id;
    navigateTo('rol-form');
}

async function guardarRol(event) {
    event.preventDefault();
    const button = document.getElementById('btn-enviar-rol');
    const error = document.getElementById('rol-error');
    error.classList.add('hidden');

    const editingId = rolEditandoId;
    const payload = {
        nombre: document.getElementById('rol-nombre').value.trim(),
        permisos: [...document.querySelectorAll('#permisos-matrix input[type="checkbox"]:checked')].map(c => c.value)
    };

    if (!payload.nombre) {
        error.textContent = 'Escribe un nombre para el rol.';
        error.classList.remove('hidden');
        return;
    }

    if (payload.nombre.length > 50) {
        error.textContent = 'El nombre del rol no puede superar 50 caracteres.';
        error.classList.remove('hidden');
        return;
    }

    if (payload.permisos.length === 0) {
        const continuar = confirm('Este rol quedará sin acceso a módulos administrativos. ¿Deseas crearlo de todos modos?');
        if (!continuar) return;
    }

    try {
        button.disabled = true;
        button.textContent = editingId ? 'Guardando cambios...' : 'Creando rol...';

        if (editingId) await apiPut(`/roles/${editingId}`, payload);
        else await apiPost('/roles', payload);

        rolEditandoId = null;
        navigateTo('roles');
        mostrarRolesFeedback(editingId
            ? `Rol “${payload.nombre}” actualizado correctamente.`
            : `Rol “${payload.nombre}” creado correctamente.`);
    } catch (err) {
        error.textContent = err.message || 'No se pudo guardar el rol.';
        error.classList.remove('hidden');
    } finally {
        button.disabled = false;
        button.textContent = editingId ? 'Guardar cambios' : 'Crear rol';
    }
}

async function eliminarRol(id) {
    if (!confirm('¿Eliminar este rol? Solo se permite si no tiene usuarios asignados.')) return;
    try {
        await apiDelete(`/roles/${id}`);
        await loadRoles();
    } catch (err) {
        alert(err.message);
    }
}
