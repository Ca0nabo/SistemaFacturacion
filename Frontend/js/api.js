const API_BASE = '/api';

function getToken() { return localStorage.getItem('token'); }
function getUser() {
    try { return JSON.parse(localStorage.getItem('user') || 'null'); }
    catch { return null; }
}
function setAuth(token, user) {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
}
function clearAuth() { localStorage.removeItem('token'); localStorage.removeItem('user'); }
function isAuthenticated() { return Boolean(getToken()); }

async function apiFetch(endpoint, options = {}) {
    const headers = { ...(options.headers || {}) };
    if (!(options.body instanceof FormData) && options.body !== undefined) headers['Content-Type'] = 'application/json';
    const token = getToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    let response;
    try {
        response = await fetch(`${API_BASE}${endpoint}`, { ...options, headers });
    } catch {
        throw new Error('No se pudo conectar con el servidor. Verifica que la API esté ejecutándose.');
    }

    if (response.status === 401 && !endpoint.startsWith('/auth/login')) {
        clearAuth();
        throw new Error('La sesión expiró. Inicia sesión nuevamente.');
    }
    if (response.status === 403) {
        throw new Error('Tu rol no tiene permiso para realizar esta acción.');
    }
    if (response.status === 204) return null;

    const contentType = response.headers.get('content-type') || '';
    const data = contentType.includes('application/json')
        ? await response.json().catch(() => null)
        : await response.text().catch(() => '');

    if (!response.ok) {
        const message = data && typeof data === 'object'
            ? (data.mensaje || data.title || Object.values(data.errors || {}).flat().join(' '))
            : data;
        throw new Error(message || `Error HTTP ${response.status}`);
    }
    return data;
}

function apiGet(endpoint) { return apiFetch(endpoint); }
function apiPost(endpoint, body) { return apiFetch(endpoint, { method: 'POST', body: JSON.stringify(body) }); }
function apiPut(endpoint, body) { return apiFetch(endpoint, { method: 'PUT', body: JSON.stringify(body) }); }
function apiPatch(endpoint, body) { return apiFetch(endpoint, { method: 'PATCH', body: JSON.stringify(body) }); }
function apiDelete(endpoint) { return apiFetch(endpoint, { method: 'DELETE' }); }
function apiPostForm(endpoint, formData) { return apiFetch(endpoint, { method: 'POST', body: formData }); }

function formatearMonto(value) {
    const number = Number(value || 0);
    return number.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function dinero(value) { return `RD$${formatearMonto(value)}`; }
function formatearFecha(value) {
    if (!value) return '—';
    const raw = String(value).length === 10 ? `${value}T00:00:00` : value;
    const date = new Date(raw);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('es-DO');
}
function formatearFechaHora(value) {
    if (!value) return '—';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-DO');
}
function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>'"]/g, char => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', "'":'&#39;', '"':'&quot;' }[char]));
}
function statusClass(value) { return `status-${String(value || '').toLowerCase().replace(/\s+/g, '-')}`; }
function queryString(params) {
    const search = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
        if (value !== '' && value !== null && value !== undefined) search.set(key, value);
    });
    const result = search.toString();
    return result ? `?${result}` : '';
}
function currentPeriod() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
}
function todayIso() { return new Date().toISOString().slice(0, 10); }
function downloadCsv(filename, rows) {
    const csv = rows.map(row => row.map(value => `"${String(value ?? '').replace(/"/g, '""')}"`).join(',')).join('\n');
    const blob = new Blob(['\ufeff', csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
}
