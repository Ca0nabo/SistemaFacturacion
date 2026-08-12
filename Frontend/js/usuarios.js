function initUsuarios() {
    onView('usuarios', loadUsuarios);

    document.getElementById('btn-nuevo-usuario').addEventListener('click', () => {
        usuarioEditandoId = null;
        navigateTo('usuario-form');
    });

    document.getElementById('btn-volver-usuarios').addEventListener('click', () => {
        usuarioEditandoId = null;
        navigateTo('usuarios');
    });

    onView('usuario-form', initUsuarioFormView);
    initUsuarioForm();
}

async function loadUsuarios() {
    const tbody = document.getElementById('usuarios-body');
    tbody.innerHTML = '<tr><td colspan="7" class="loading">Cargando usuarios...</td></tr>';

    try {
        const usuarios = await apiGet('/users');
        const userActual = getUser();
        tbody.innerHTML = (usuarios || []).map(u => {
            const est = u.activo ? 'activo' : 'inactivo';
            const esYo = userActual && userActual.id === u.idUsuario;
            return `
            <tr>
                <td>${u.idUsuario}</td>
                <td>${u.nombreCompleto}${esYo ? ' <strong>(tu)</strong>' : ''}</td>
                <td>${u.email}</td>
                <td><span class="rol-badge">${u.rol}</span></td>
                <td><span class="status-badge status-${est}">${u.activo ? 'Activo' : 'Inactivo'}</span></td>
                <td>${new Date(u.fechaCreacion).toLocaleDateString('es-DO')}</td>
                <td class="acciones">${hasPermission('USUARIOS.GESTIONAR') ? `
                    <button class="btn btn-sm btn-secondary" onclick="editarUsuario(${u.idUsuario})">Editar</button>
                    <button class="btn btn-sm ${u.activo ? 'btn-danger' : 'btn-primary'}" onclick="toggleUsuario(${u.idUsuario})">${u.activo ? 'Desactivar' : 'Activar'}</button>` : '<span class="muted">Solo lectura</span>'}</td>
            </tr>`;
        }).join('');
    } catch (err) {
        tbody.innerHTML = '<tr><td colspan="7" style="color:var(--red);text-align:center">' + err.message + '</td></tr>';
    }
}

let usuarioEditandoId = null;

function initUsuarioFormView() {
    if (!usuarioEditandoId) {
        document.getElementById('usuario-form').reset();
        document.getElementById('usuario-password').required = true;
        document.getElementById('btn-enviar-usuario').textContent = 'Crear Usuario';
        document.querySelector('#view-usuario-form .page-header h1').textContent = 'Nuevo Usuario';
    }
    cargarRoles();
}

async function cargarRoles(selectedId) {
    const select = document.getElementById('usuario-rol');
    select.innerHTML = '<option value="">Cargando roles...</option>';

    try {
        const roles = await apiGet('/users/roles');
        select.innerHTML = '<option value="">Seleccione un rol...</option>';
        roles.forEach(r => {
            const sel = r.idRol === selectedId ? 'selected' : '';
            select.innerHTML += '<option value="' + r.idRol + '" ' + sel + '>' + r.nombre + '</option>';
        });
    } catch (err) {
        select.innerHTML = '<option value="">Error cargando roles</option>';
    }
}

function initUsuarioForm() {
    const form = document.getElementById('usuario-form');
    const btnEnviar = document.getElementById('btn-enviar-usuario');
    const errorDiv = document.getElementById('usuario-error');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Guardando...';

        const payload = {
            nombreCompleto: document.getElementById('usuario-nombre').value.trim(),
            email: document.getElementById('usuario-email').value.trim(),
            password: document.getElementById('usuario-password').value || null,
            idRol: parseInt(document.getElementById('usuario-rol').value)
        };

        if (!payload.nombreCompleto || !payload.email || !payload.idRol) {
            errorDiv.textContent = 'Complete todos los campos obligatorios.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Guardar';
            return;
        }

        try {
            if (usuarioEditandoId) await apiPut(`/users/${usuarioEditandoId}`, payload);
            else await apiPost('/users', payload);
            usuarioEditandoId = null;
            navigateTo('usuarios');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Guardar';
        }
    });
}

async function editarUsuario(id) {
    try {
        const u = await apiGet('/users/' + id);
        usuarioEditandoId = id;
        document.getElementById('usuario-form').reset();
        document.getElementById('usuario-id').value = u.idUsuario;
        document.getElementById('usuario-nombre').value = u.nombreCompleto;
        document.getElementById('usuario-email').value = u.email;
        document.getElementById('usuario-password').required = false;
        document.getElementById('usuario-password').placeholder = 'Dejar vacio para no cambiar';
        document.getElementById('btn-enviar-usuario').textContent = 'Actualizar Usuario';
        document.querySelector('#view-usuario-form .page-header h1').textContent = 'Editar Usuario';
        await cargarRoles(u.idRol);
        navigateTo('usuario-form');
    } catch (err) {
        alert('Error: ' + err.message);
    }
}

async function toggleUsuario(id) {
    if (!confirm('Esta seguro de cambiar el estado de este usuario?')) return;
    try {
        await apiPatch('/users/' + id + '/estado');
        loadUsuarios();
    } catch (err) {
        alert('Error: ' + err.message);
    }
}
