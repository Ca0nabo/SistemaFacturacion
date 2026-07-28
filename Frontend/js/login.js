function initLogin() {
    const form = document.getElementById('login-form');
    const emailInput = document.getElementById('login-email');
    const passwordInput = document.getElementById('login-password');
    const errorDiv = document.getElementById('login-error');
    const btnLogin = document.getElementById('btn-login');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorDiv.classList.add('hidden');
        btnLogin.disabled = true;
        btnLogin.textContent = 'Iniciando sesión...';

        try {
            const data = await apiPost('/auth/login', {
                email: emailInput.value.trim(),
                password: passwordInput.value
            });

            setAuth(data.token, {
                id: data.idUsuario,
                email: data.email,
                nombre: data.nombreCompleto,
                rol: data.rol
            });

            showApp();
            navigateTo('dashboard');
        } catch (err) {
            errorDiv.textContent = err.message;
            errorDiv.classList.remove('hidden');
        } finally {
            btnLogin.disabled = false;
            btnLogin.textContent = 'Iniciar Sesión';
        }
    });
}
