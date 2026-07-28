function initReportes() {
    onView('reportes', loadReportes);
}

async function loadReportes() {
    await Promise.all([
        cargarReporteIngresos(),
        cargarReporteClientes(),
        cargarReporteCxC()
    ]);
}

async function cargarReporteIngresos() {
    const tbody = document.getElementById('reporte-ingresos-body');
    tbody.innerHTML = '<tr><td colspan="3" class="loading">Cargando...</td></tr>';

    try {
        const data = await apiGet('/reportes/ingresos-mensuales');

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" style="text-align:center;color:var(--gray-500)">Sin datos para el año actual.</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(r => `
            <tr>
                <td>${r.mesNombre ?? r.mes ?? ''}</td>
                <td>RD$${formatearMonto(r.totalIngresos ?? 0)}</td>
                <td>${r.cantidadFacturas ?? 0}</td>
            </tr>
        `).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="3" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

async function cargarReporteClientes() {
    const tbody = document.getElementById('reporte-clientes-body');
    tbody.innerHTML = '<tr><td colspan="4" class="loading">Cargando...</td></tr>';

    try {
        const data = await apiGet('/reportes/facturacion-por-cliente');

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" style="text-align:center;color:var(--gray-500)">Sin datos de facturación.</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(r => `
            <tr>
                <td>${r.razonSocial ?? r.cliente ?? ''}</td>
                <td>${r.rncCedula ?? ''}</td>
                <td>RD$${formatearMonto(r.totalFacturado ?? 0)}</td>
                <td>${r.cantidadFacturas ?? 0}</td>
            </tr>
        `).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="4" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

async function cargarReporteCxC() {
    const container = document.getElementById('reporte-cxc-metrics');

    try {
        const data = await apiGet('/reportes/estado-cuentas');

        container.innerHTML = `
            <div class="metric-card">
                <div class="metric-icon" style="background:#e8f5e9"><span>✅</span></div>
                <div class="metric-info">
                    <span class="metric-label">Pagado</span>
                    <span class="metric-value">RD$${formatearMonto(data.totalPagado ?? 0)}</span>
                </div>
            </div>
            <div class="metric-card">
                <div class="metric-icon" style="background:#e3f2fd"><span>⏳</span></div>
                <div class="metric-info">
                    <span class="metric-label">Pendiente (${data.cantidadPendiente ?? 0})</span>
                    <span class="metric-value">RD$${formatearMonto(data.totalPendiente ?? 0)}</span>
                </div>
            </div>
            <div class="metric-card">
                <div class="metric-icon" style="background:#fce4ec"><span>⚠️</span></div>
                <div class="metric-info">
                    <span class="metric-label">Vencido (${data.cantidadVencido ?? 0})</span>
                    <span class="metric-value">RD$${formatearMonto(data.totalVencido ?? 0)}</span>
                </div>
            </div>
        `;
    } catch (err) {
        container.innerHTML = `<p style="color:var(--red)">Error: ${err.message}</p>`;
    }
}
