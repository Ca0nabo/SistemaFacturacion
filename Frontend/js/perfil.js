function initPerfil() {
    onView('perfil', loadPerfil);
    initPerfilForm();
    initPasswordForm();
}

async function loadPerfil() {
    try {
        const userData = await apiGet('/auth/me');
        document.getElementById('perfil-nombre').value = userData.nombreCompleto || '';
        document.getElementById('perfil-email').value = userData.email || '';
    } catch (err) {
        console.error('Error cargando perfil:', err);
    }
}

function initPerfilForm() {
    const form = document.getElementById('perfil-form');
    const btnEnviar = document.getElementById('btn-enviar-perfil');
    const errorDiv = document.getElementById('perfil-error');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Actualizando...';

        const payload = {
            nombreCompleto: document.getElementById('perfil-nombre').value.trim(),
            email: document.getElementById('perfil-email').value.trim()
        };

        if (!payload.nombreCompleto || !payload.email) {
            errorDiv.textContent = 'Complete todos los campos.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Actualizar Perfil';
            return;
        }

        try {
            const result = await apiPut('/auth/perfil', payload);
            const user = getUser();
            if (user) {
                setAuth(result.token, {
                    ...user,
                    id: result.idUsuario,
                    email: result.email,
                    nombre: result.nombreCompleto,
                    rol: result.rol,
                    permisos: result.permisos ?? user.permisos ?? []
                });
                updateUserChrome();
                applyPermissionVisibility();
            }
            errorDiv.classList.add('hidden');
            alert('Perfil actualizado exitosamente.');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Actualizar Perfil';
        }
    });
}

function initPasswordForm() {
    const form = document.getElementById('password-form');
    const btnEnviar = document.getElementById('btn-enviar-password');
    const errorDiv = document.getElementById('password-error');
    const successDiv = document.getElementById('password-success');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        successDiv.classList.add('hidden');
        btnEnviar.disabled = true;
        btnEnviar.textContent = 'Cambiando...';

        const payload = {
            passwordActual: document.getElementById('pass-actual').value,
            nuevaPassword: document.getElementById('pass-nueva').value
        };

        if (!payload.passwordActual || !payload.nuevaPassword) {
            errorDiv.textContent = 'Complete todos los campos.';
            errorDiv.classList.remove('hidden');
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Cambiar Contrasena';
            return;
        }

        try {
            await apiPost('/auth/cambiar-password', payload);
            form.reset();
            successDiv.textContent = 'Contrasena actualizada exitosamente.';
            successDiv.classList.remove('hidden');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnEnviar.disabled = false;
            btnEnviar.textContent = 'Cambiar Contrasena';
        }
    });
}
