window.viewTitles = {
    dashboard: 'Dashboard',
    propietarios: 'Propietarios',
    clientes: 'Inquilinos',
    proveedores: 'Proveedores',
    'entidad-form': 'Entidad',
    propiedades: 'Propiedades',
    'propiedad-form': 'Propiedad',
    contratos: 'Contratos',
    'contrato-form': 'Contrato',
    facturas: 'Facturación mensual',
    'factura-form': 'Nueva factura mensual',
    depositos: 'Depósitos de garantía',
    'deposito-form': 'Depósito',
    'acuerdos-pago': 'Acuerdos de pago',
    'acuerdo-form': 'Acuerdo de pago',
    movimientos: 'Movimientos',
    gastos: 'Gastos',
    reportes: 'Reportes',
    usuarios: 'Usuarios',
    'usuario-form': 'Usuario',
    roles: 'Roles y permisos',
    'rol-form': 'Rol y permisos',
    perfil: 'Mi perfil',
    auditoria: 'Auditoría',
    'access-denied': 'Acceso restringido'
};

window.viewPermissions = {
    dashboard: 'DASHBOARD.VER',

    propietarios: 'ENTIDADES.VER',
    clientes: 'ENTIDADES.VER',
    proveedores: 'ENTIDADES.VER',
    'entidad-form': 'ENTIDADES.GESTIONAR',

    propiedades: 'PROPIEDADES.VER',
    'propiedad-form': 'PROPIEDADES.GESTIONAR',

    contratos: 'CONTRATOS.VER',
    'contrato-form': 'CONTRATOS.GESTIONAR',

    facturas: 'FACTURAS.VER',
    'factura-form': 'FACTURAS.CREAR',

    depositos: 'DEPOSITOS.VER',
    'deposito-form': 'DEPOSITOS.GESTIONAR',

    'acuerdos-pago': 'ACUERDOS.VER',
    'acuerdo-form': 'ACUERDOS.GESTIONAR',

    movimientos: 'MOVIMIENTOS.VER',

    gastos: 'GASTOS.GESTIONAR',

    reportes: 'REPORTES.VER',

    usuarios: 'USUARIOS.VER',
    'usuario-form': 'USUARIOS.GESTIONAR',

    roles: 'ROLES.VER',
    'rol-form': 'ROLES.GESTIONAR',

    auditoria: 'AUDITORIA.VER',

    perfil: null,
    'access-denied': null
};

let alertasInterval = null;

function hasPermission(permission) {
    if (!permission) {
        return true;
    }

    const user = getUser();

    if (!user) {
        return false;
    }

    if (user.rol === 'Administrador') {
        return true;
    }

    return Array.isArray(user.permisos)
        && user.permisos.includes(permission);
}

function canAccessView(viewName) {
    return hasPermission(
        window.viewPermissions[viewName]
    );
}

function getDefaultView() {
    const priority = [
        'dashboard',
        'facturas',
        'movimientos',
        'contratos',
        'propiedades',
        'clientes',
        'reportes',
        'usuarios',
        'roles',
        'perfil'
    ];

    return priority.find(canAccessView) || 'perfil';
}

function applyPermissionVisibility(root = document) {
    root.querySelectorAll('[data-permission]')
        .forEach(element => {
            const allowed = hasPermission(
                element.dataset.permission
            );

            element.classList.toggle(
                'permission-hidden',
                !allowed
            );

            element.setAttribute(
                'aria-hidden',
                allowed ? 'false' : 'true'
            );

            if ('disabled' in element && !allowed) {
                element.disabled = true;
            }
        });
}

function getUserInitials(name) {
    return String(name || 'Usuario')
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map(part => part[0])
        .join('')
        .toUpperCase() || 'HC';
}

function closeTopbarUserMenu() {
    const menu =
        document.getElementById('topbar-user-menu');

    const chip =
        document.getElementById('topbar-user-chip');

    if (menu) {
        menu.classList.add('hidden');
    }

    if (chip) {
        chip.setAttribute(
            'aria-expanded',
            'false'
        );
    }
}

function toggleTopbarUserMenu() {
    const menu =
        document.getElementById('topbar-user-menu');

    const chip =
        document.getElementById('topbar-user-chip');

    if (!menu || !chip) {
        return;
    }

    const willOpen =
        menu.classList.contains('hidden');

    document
        .getElementById('alertas-dropdown')
        ?.classList.add('hidden');

    menu.classList.toggle(
        'hidden',
        !willOpen
    );

    chip.setAttribute(
        'aria-expanded',
        willOpen ? 'true' : 'false'
    );
}

function logoutCurrentUser() {
    closeTopbarUserMenu();

    clearAuth();

    window.location.reload();
}

function updateUserChrome() {
    const user = getUser();

    const name =
        user?.nombre || 'Usuario';

    const role =
        user?.rol || 'Sin rol';

    const email =
        user?.email || '';

    const value =
        getUserInitials(name);

    const topName =
        document.getElementById('topbar-user-name');

    if (topName) {
        topName.textContent = name;
    }

    const topRole =
        document.getElementById('topbar-user-role');

    if (topRole) {
        topRole.textContent = role;
    }

    const initials =
        document.getElementById(
            'topbar-user-initials'
        );

    if (initials) {
        initials.textContent = value;
    }

    const menuInitials =
        document.getElementById(
            'topbar-menu-initials'
        );

    if (menuInitials) {
        menuInitials.textContent = value;
    }

    const menuName =
        document.getElementById(
            'topbar-menu-name'
        );

    if (menuName) {
        menuName.textContent = name;
    }

    const menuEmail =
        document.getElementById(
            'topbar-menu-email'
        );

    if (menuEmail) {
        menuEmail.textContent = email;
    }

    const menuRole =
        document.getElementById(
            'topbar-menu-role'
        );

    if (menuRole) {
        menuRole.textContent = role;
    }
}

function showApp() {
    document
        .getElementById('sidebar')
        .classList.remove('hidden');

    document
        .getElementById('top-bar')
        .classList.remove('hidden');

    document
        .getElementById('view-login')
        .classList.add('hidden');

    updateUserChrome();

    applyPermissionVisibility();
}

function showLogin() {
    closeTopbarUserMenu();

    if (alertasInterval) {
        clearInterval(alertasInterval);
    }

    document
        .getElementById('sidebar')
        .classList.add('hidden');

    document
        .getElementById('top-bar')
        .classList.add('hidden');

    document
        .querySelectorAll('.view')
        .forEach(view =>
            view.classList.add('hidden')
        );

    document
        .getElementById('view-login')
        .classList.remove('hidden');

    currentView = 'login';
}

function showAccessDenied(viewName) {
    const permission =
        window.viewPermissions[viewName];

    const message =
        document.getElementById(
            'access-denied-message'
        );

    if (message) {
        message.textContent = permission
            ? `Tu rol no tiene el permiso ${permission} necesario para abrir este módulo.`
            : 'Tu rol no tiene acceso a esta funcionalidad.';
    }

    document
        .querySelectorAll('.view')
        .forEach(v =>
            v.classList.add('hidden')
        );

    document
        .getElementById('view-access-denied')
        ?.classList.remove('hidden');

    currentView = 'access-denied';

    const title =
        document.getElementById('view-title');

    if (title) {
        title.textContent =
            'Acceso restringido';
    }
}

function safeInit(name, fn) {
    try {
        if (typeof fn === 'function') {
            fn();
        }
    } catch (error) {
        console.error(
            `No se pudo inicializar ${name}:`,
            error
        );
    }
}

async function performAutoLogin() {
    const data =
        await apiGet('/auth/auto-login');

    setAuth(
        data.token,
        {
            id: data.idUsuario,
            nombre: data.nombreCompleto,
            email: data.email,
            rol: data.rol,
            permisos: data.permisos ?? []
        }
    );

    showApp();

    navigateTo(
        getDefaultView()
    );
}

document.addEventListener(
    'DOMContentLoaded',
    async () => {
        initRouter();

        safeInit('login', initLogin);
        safeInit('dashboard', initDashboard);
        safeInit('entidades', initClientes);
        safeInit('propiedades', initPropiedades);
        safeInit('unidades', initUnidades);
        safeInit('contratos', initContratos);
        safeInit('facturas', initFacturas);
        safeInit('depósitos', initDepositos);
        safeInit('acuerdos', initAcuerdos);
        safeInit('movimientos', initMovimientos);
        safeInit('gastos', initGastos);
        safeInit('reportes', initReportes);
        safeInit('usuarios', initUsuarios);
        safeInit('roles', initRoles);
        safeInit('perfil', initPerfil);
        safeInit('auditoría', initAuditoria);
        safeInit('alertas', initAlertas);

        document
            .getElementById(
                'btn-access-home'
            )
            ?.addEventListener(
                'click',
                () => navigateTo(
                    getDefaultView()
                )
            );

        const token = getToken();

        if (token) {
            try {
                const data =
                    await apiGet('/auth/me');

                const user =
                    getUser() || {};

                setAuth(
                    token,
                    {
                        ...user,

                        id:
                            data.idUsuario
                            ?? user.id,

                        nombre:
                            data.nombreCompleto
                            ?? user.nombre,

                        email:
                            data.email
                            ?? user.email,

                        rol:
                            data.rol
                            ?? user.rol,

                        permisos:
                            data.permisos
                            ?? []
                    }
                );

                showApp();

                navigateTo(
                    getDefaultView()
                );
            } catch {
                clearAuth();

                try {
                    await performAutoLogin();
                } catch {
                    showLogin();
                }
            }
        } else {
            try {
                await performAutoLogin();
            } catch {
                showLogin();
            }
        }

        document
            .getElementById(
                'btn-topbar-logout'
            )
            ?.addEventListener(
                'click',
                logoutCurrentUser
            );

        document
            .getElementById(
                'btn-topbar-profile'
            )
            ?.addEventListener(
                'click',
                () => {
                    closeTopbarUserMenu();

                    navigateTo('perfil');
                }
            );

        document
            .getElementById(
                'topbar-user-chip'
            )
            ?.addEventListener(
                'click',
                event => {
                    event.stopPropagation();

                    toggleTopbarUserMenu();
                }
            );

        document.addEventListener(
            'click',
            event => {
                const wrap =
                    document.getElementById(
                        'topbar-user-menu-wrap'
                    );

                if (
                    wrap
                    && !wrap.contains(
                        event.target
                    )
                ) {
                    closeTopbarUserMenu();
                }
            }
        );

        document.addEventListener(
            'keydown',
            event => {
                if (event.key === 'Escape') {
                    closeTopbarUserMenu();
                }
            }
        );
    }
);

document.addEventListener(
    'click',
    event => {
        const button =
            event.target.closest(
                '[data-toggle-password]'
            );

        if (!button) {
            return;
        }

        const input =
            button.parentElement
                .querySelector('input');

        if (!input) {
            return;
        }

        input.type =
            input.type === 'password'
                ? 'text'
                : 'password';

        button.textContent =
            input.type === 'password'
                ? '👁'
                : '🙈';
    }
);