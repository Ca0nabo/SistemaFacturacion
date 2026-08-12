let currentView = null;
const viewCallbacks = {};

function navigateTo(viewName) {
    if (viewName !== 'access-denied' && !canAccessView(viewName)) {
        showAccessDenied(viewName);
        return;
    }
    if (currentView === viewName) return;

    document.querySelectorAll('.view').forEach(v => v.classList.add('hidden'));
    const target = document.getElementById(`view-${viewName}`);
    if (!target) return;
    target.classList.remove('hidden');

    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    const navItem = document.querySelector(`.nav-item[data-view="${viewName}"]`);
    if (navItem) navItem.classList.add('active');

    currentView = viewName;

    const titleEl = document.getElementById('view-title');
    if (titleEl && window.viewTitles?.[viewName]) titleEl.textContent = window.viewTitles[viewName];

    applyPermissionVisibility(target);
    if (viewCallbacks[viewName]) {
        Promise.resolve(viewCallbacks[viewName]()).then(() => applyPermissionVisibility(target)).catch(err => console.error('Error en vista', viewName, err));
    }
}

function onView(viewName, callback) {
    viewCallbacks[viewName] = callback;
}

function initRouter() {
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            navigateTo(item.dataset.view);
        });
    });
}
