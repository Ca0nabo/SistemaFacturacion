const viewTitles = {
    dashboard: 'Dashboard',
    facturas: 'Facturas',
    'factura-form': 'Nueva Factura',
    contratos: 'Contratos',
    'contrato-form': 'Nuevo Contrato',
    propiedades: 'Propiedades',
    'propiedad-form': 'Propiedad',
    clientes: 'Clientes',
    proveedores: 'Proveedores',
    'entidad-form': 'Entidad',
    movimientos: 'Cuentas por Cobrar / Pagar',
    gastos: 'Registro de Gastos',
    'acuerdos-pago': 'Acuerdos de Pago',
    reportes: 'Reportes',
    auditoria: 'Auditoría',
    usuarios: 'Usuarios'
};

let alertasInterval = null;

function showApp() {
    document.getElementById('sidebar').classList.remove('hidden');
    document.getElementById('view-login').classList.add('hidden');
    document.getElementById('top-bar').classList.remove('hidden');
    const user = getUser();
    if (user) {
        document.getElementById('user-info').textContent = `${user.nombre} · ${user.rol}`;
    }
    aplicarVisibilidadPorRol();
}

function showLogin() {
    if (alertasInterval) { clearInterval(alertasInterval); alertasInterval = null; }
    document.getElementById('sidebar').classList.add('hidden');
    document.getElementById('top-bar').classList.add('hidden');
    document.querySelectorAll('.view').forEach(v => v.classList.add('hidden'));
    document.getElementById('view-login').classList.remove('hidden');
    currentView = 'login';
}

function aplicarVisibilidadPorRol() {
    const user = getUser();
    if (!user) return;
    const esAdmin = user.rol === 'Administrador';

    document.querySelectorAll('.nav-item[data-view]').forEach(item => {
        const view = item.dataset.view;
        if (view === 'usuarios' || view === 'auditoria') {
            item.style.display = esAdmin ? '' : 'none';
        }
    });
}

function hasPermission(modulo) {
    const user = getUser();
    if (!user) return false;
    if (user.rol === 'Administrador') return true;
    const permisosMap = {
        'Administrador': 'TODO',
        'Contador': 'FACTURAS,REPORTES,MOVIMIENTOS',
        'Encargado de facturaci\u00f3n': 'FACTURAS,ENTIDADES',
        'Gerente financiero': 'REPORTES,CONTRATOS,MOVIMIENTOS',
        'Cliente': 'CONSULTA',
        'Proveedor': 'CONSULTA'
    };
    const permisos = permisosMap[user.rol] || '';
    return permisos === 'TODO' || permisos.split(',').includes(modulo);
}

function actualizarMenuLateral() {
    const user = getUser();
    if (!user) return;
    aplicarVisibilidadPorRol();
}

document.addEventListener('DOMContentLoaded', async () => {
    initRouter();
    initLogin();
    initDashboard();
    initFacturas();
    initClientes();
    initMovimientos();
    initContratos();
    initPropiedades();
    initUnidades();
    initGastos();
    initAcuerdos();
    initReportes();
    initAuditoria();
    initAlertas();
    initUsuarios();
    initPerfil();

    const token = getToken();
    if (token) {
        try {
            const userData = await apiGet('/auth/me');
            const user = getUser();
            if (user) {
                setAuth(token, { ...user, ...userData });
            }
            showApp();
            navigateTo('dashboard');
        } catch {
            clearAuth();
            showLogin();
        }
    } else {
        showLogin();
    }

    document.getElementById('btn-logout').addEventListener('click', () => {
        clearAuth();
        if (alertasInterval) { clearInterval(alertasInterval); alertasInterval = null; }
        window.location.reload();
    });
});

document.addEventListener('click', function(e) {
    const btn = e.target.closest('[data-toggle-password]');
    if (!btn) return;
    const input = btn.parentElement.querySelector('input');
    const isPassword = input.type === 'password';
    input.type = isPassword ? 'text' : 'password';
    btn.textContent = isPassword ? '🙈' : '👁️';
});
