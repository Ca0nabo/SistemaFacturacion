let dashboardPeriodoActual = currentPeriod();

function initDashboard() {
    onView('dashboard', loadDashboard);

    document.querySelectorAll('[data-dashboard-target]').forEach(card => {
        card.addEventListener('click', () => abrirDetalleDashboard(card.dataset.dashboardTarget));
    });
}

async function loadDashboard() {
    const fecha = document.getElementById('dashboard-date');
    fecha.textContent = new Date().toLocaleDateString('es-DO', {
        weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
    });

    const subtitle = document.getElementById('dashboard-period-label');
    if (subtitle) subtitle.textContent = 'Cargando información del mes...';

    try {
        const metricas = await apiGet('/dashboard/metricas');
        dashboardPeriodoActual = metricas.periodoActual || currentPeriod();

        document.getElementById('metric-ingresos').textContent = dinero(metricas.facturadoMes);
        document.getElementById('metric-cobrado').textContent = dinero(metricas.cobradoMes);
        document.getElementById('metric-gastos').textContent = dinero(metricas.gastosMes);
        document.getElementById('metric-cxc').textContent = dinero(metricas.totalCxC);
        document.getElementById('metric-cxp').textContent = dinero(metricas.totalCxP);
        document.getElementById('metric-ocupacion').textContent = `${metricas.tasaOcupacion || 0}%`;
        document.getElementById('metric-contratos').textContent = metricas.contratosActivos || 0;
        document.getElementById('metric-contratos-vencer').textContent = metricas.contratosPorVencer || 0;
        document.getElementById('metric-margen').textContent = dinero(metricas.margenGanancia);

        if (subtitle) {
            subtitle.textContent = `Resultados de ${formatearPeriodoDashboard(dashboardPeriodoActual)} · ${metricas.facturasEmitidas || 0} factura(s) del período`;
        }

        renderUltimasFacturas(metricas.ultimasFacturas || []);
        renderChart(metricas.tendenciaMensual || []);
    } catch (error) {
        console.error('Dashboard:', error);
        if (subtitle) subtitle.textContent = 'No fue posible cargar el resumen.';
        document.getElementById('dashboard-facturas-body').innerHTML =
            `<tr><td colspan="6" class="table-error">${escapeHtml(error.message)}</td></tr>`;
    }
}

function renderUltimasFacturas(facturas) {
    const tbody = document.getElementById('dashboard-facturas-body');
    const rows = (facturas || []).slice(0, 6);

    if (!rows.length) {
        tbody.innerHTML = '<tr><td colspan="6" class="empty-state">Aún no hay facturas.</td></tr>';
        return;
    }

    tbody.innerHTML = rows.map(f => {
        const estadoVisual = f.esPeriodoFuturo ? 'FUTURA' : f.estado;
        const tipo = String(f.tipoFactura || '').toUpperCase() === 'CREDITO' ? 'A crédito' : 'Al contado';
        return `<tr class="dashboard-invoice-row${f.esPeriodoFuturo ? ' invoice-future-row' : ''}">
            <td><strong>${escapeHtml(f.numeroECF)}</strong><br><span class="muted">${escapeHtml(formatearPeriodoDashboard(f.periodoFacturado) || 'Manual')}</span></td>
            <td>${escapeHtml(f.razonSocial)}</td>
            <td><span class="invoice-type-badge ${tipo === 'A crédito' ? 'invoice-type-credit' : 'invoice-type-cash'}">${tipo}</span></td>
            <td class="dashboard-money-cell"><strong>${dinero(f.total)}</strong><br><span class="muted">Pend. ${dinero(f.montoPendiente)}</span></td>
            <td><span class="status-badge ${statusClass(estadoVisual)}">${escapeHtml(estadoVisual)}</span></td>
            <td><button class="btn btn-sm btn-secondary" type="button" onclick="verFactura(${f.idFactura})">Ver</button></td>
        </tr>`;
    }).join('');
}

function renderChart(series) {
    const container = document.getElementById('chart-bars');
    const months = (series || []).map(item => ({
        periodo: item.periodo,
        label: etiquetaMesDashboard(item.periodo),
        billed: Number(item.facturado || 0),
        paid: Number(item.cobrado || 0)
    }));

    if (!months.length) {
        container.innerHTML = '<div class="empty-state chart-empty">No hay datos para graficar.</div>';
        return;
    }

    const max = Math.max(1, ...months.flatMap(m => [m.billed, m.paid]));
    const alturaMaxima = 170;

    container.innerHTML = months.map(m => {
        const billedHeight = m.billed <= 0 ? 0 : Math.max(5, (m.billed / max) * alturaMaxima);
        const paidHeight = m.paid <= 0 ? 0 : Math.max(5, (m.paid / max) * alturaMaxima);
        return `<div class="chart-bar-group">
            <div class="chart-pair">
                <div class="chart-bar ingresos" style="height:${billedHeight}px" title="Facturado: ${dinero(m.billed)}" aria-label="${m.label}, facturado ${dinero(m.billed)}"></div>
                <div class="chart-bar cobrado" style="height:${paidHeight}px" title="Cobrado: ${dinero(m.paid)}" aria-label="${m.label}, cobrado ${dinero(m.paid)}"></div>
            </div>
            <span class="chart-label">${escapeHtml(m.label)}</span>
        </div>`;
    }).join('');
}

function abrirDetalleDashboard(target) {
    switch (target) {
        case 'facturas-current': {
            const periodo = document.getElementById('facturas-filter-periodo');
            const estado = document.getElementById('facturas-filter-estado');
            if (periodo) periodo.value = dashboardPeriodoActual;
            if (estado) estado.value = '';
            navigateTo('facturas');
            break;
        }
        case 'cobros-current':
        case 'cxc': {
            prepararFechasMovimientos(target === 'cobros-current');
            navigateTo('movimientos');
            break;
        }
        case 'gastos':
        case 'cxp':
            navigateTo('gastos');
            break;
        case 'ocupacion': {
            const estadoPropiedad = document.getElementById('filter-estado-propiedad');
            if (estadoPropiedad) estadoPropiedad.value = 'Alquilada';
            navigateTo('propiedades');
            break;
        }
        case 'contratos':
        case 'contratos-vencer':
            navigateTo('contratos');
            break;
        default:
            break;
    }
}

function prepararFechasMovimientos(limitarAlMes) {
    const desde = document.getElementById('mov-filter-desde');
    const hasta = document.getElementById('mov-filter-hasta');
    if (!desde || !hasta) return;

    if (!limitarAlMes) {
        desde.value = '';
        hasta.value = '';
        return;
    }

    const [year, month] = dashboardPeriodoActual.split('-').map(Number);
    const ultimoDia = new Date(year, month, 0).getDate();
    desde.value = `${year}-${String(month).padStart(2, '0')}-01`;
    hasta.value = `${year}-${String(month).padStart(2, '0')}-${String(ultimoDia).padStart(2, '0')}`;
}

function formatearPeriodoDashboard(periodo) {
    if (!periodo || !/^\d{4}-\d{2}$/.test(periodo)) return periodo || '';
    const [year, month] = periodo.split('-').map(Number);
    return new Date(year, month - 1, 1).toLocaleDateString('es-DO', {
        month: 'long', year: 'numeric'
    });
}

function etiquetaMesDashboard(periodo) {
    if (!periodo || !/^\d{4}-\d{2}$/.test(periodo)) return periodo || '';
    const [year, month] = periodo.split('-').map(Number);
    return new Date(year, month - 1, 1)
        .toLocaleDateString('es-DO', { month: 'short' })
        .replace('.', '');
}
