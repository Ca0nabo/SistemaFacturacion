const API_BASE = 'http://localhost:5260/api';

function getToken() {
    return localStorage.getItem('token');
}

function getUser() {
    try {
        const stored = localStorage.getItem('user');
        return stored ? JSON.parse(stored) : null;
    } catch {
        return null;
    }
}

function setAuth(token, user) {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
}

function clearAuth() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
}

function isAuthenticated() {
    return !!getToken();
}

async function apiFetch(endpoint, options = {}) {
    const url = `${API_BASE}${endpoint}`;
    const headers = { ...options.headers };

    if (!(options.body instanceof FormData)) {
        headers['Content-Type'] = 'application/json';
    }

    const token = getToken();
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(url, { ...options, headers });

    if (response.status === 401 && !endpoint.startsWith('/auth/login')) {
        clearAuth();
        throw new Error('Sesión expirada. Inicia sesión nuevamente.');
    }

    if (response.status === 204) return null;

    if (!response.ok) {
        const error = await response.json().catch(() => ({ mensaje: `Error HTTP ${response.status}` }));
        throw new Error(error.mensaje || `Error HTTP ${response.status}`);
    }

    return response.json();
}

function apiGet(endpoint) { return apiFetch(endpoint); }
function apiPost(endpoint, body) { return apiFetch(endpoint, { method: 'POST', body: JSON.stringify(body) }); }
function apiPut(endpoint, body) { return apiFetch(endpoint, { method: 'PUT', body: JSON.stringify(body) }); }
function apiDelete(endpoint) { return apiFetch(endpoint, { method: 'DELETE' }); }
function apiPatch(endpoint, body) { return apiFetch(endpoint, { method: 'PATCH', body: JSON.stringify(body) }); }
function apiPostForm(endpoint, formData) { return apiFetch(endpoint, { method: 'POST', body: formData }); }
