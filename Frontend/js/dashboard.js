function initDashboard() {
    onView('dashboard', loadDashboard);
}

async function loadDashboard() {
    const dateEl = document.getElementById('dashboard-date');
    dateEl.textContent = new Date().toLocaleDateString('es-ES', {
        year: 'numeric', month: 'long', day: 'numeric'
    });

    try {
        const [facturas, movimientos, metricas] = await Promise.all([
            apiGet('/facturacion'),
            apiGet('/movimientos'),
            apiGet('/dashboard/metricas').catch(() => null)
        ]);

        const facturasArr = facturas || [];
        const movimientosArr = movimientos || [];

        calcularMetricas(facturasArr, movimientosArr, metricas);
        renderUltimasFacturas(facturasArr);
        renderChart(facturasArr);
    } catch (err) {
        console.error('Error cargando dashboard:', err);
    }
}

function calcularMetricas(facturas, movimientos, metricas) {
    const now = new Date();
    const mesActual = now.getMonth();
    const anioActual = now.getFullYear();

    const facturasMes = (facturas || []).filter(f => {
        const d = new Date(f.fechaEmision);
        if (isNaN(d.getTime())) return false;
        return d.getMonth() === mesActual && d.getFullYear() === anioActual;
    });

    const ingresos = facturasMes
        .filter(f => f.estado === 'EMITIDA')
        .reduce((sum, f) => sum + (f.total ?? 0), 0);

    const gastos = (movimientos || [])
        .filter(m => m.tipo === 'CxP' && m.montoPendiente > 0)
        .reduce((sum, m) => sum + (m.montoPendiente ?? 0), 0);

    const cxc = (movimientos || [])
        .filter(m => m.tipo === 'CxC' && m.montoPendiente > 0)
        .reduce((sum, m) => sum + (m.montoPendiente ?? 0), 0);

    const cxp = (movimientos || [])
        .filter(m => m.tipo === 'CxP' && m.montoPendiente > 0)
        .reduce((sum, m) => sum + (m.montoPendiente ?? 0), 0);

    document.getElementById('metric-ingresos').textContent = `RD$${formatearMonto(ingresos)}`;
    document.getElementById('metric-gastos').textContent = `RD$${formatearMonto(gastos)}`;
    document.getElementById('metric-cxc').textContent = `RD$${formatearMonto(cxc)}`;
    document.getElementById('metric-cxp').textContent = `RD$${formatearMonto(cxp)}`;

    if (metricas && metricas.margenGanancia !== undefined) {
        document.getElementById('metric-margen').textContent = `RD$${formatearMonto(metricas.margenGanancia)}`;
    } else {
        const margen = ingresos > 0 ? ((ingresos - gastos) / ingresos) * 100 : 0;
        document.getElementById('metric-margen').textContent = `${formatearMonto(margen)}%`;
    }

    if (metricas) {
        document.getElementById('metric-ocupacion').textContent = `${metricas.tasaOcupacion ?? 0}%`;
        document.getElementById('metric-contratos').textContent = metricas.contratosActivos ?? 0;
        document.getElementById('metric-contratos-vencer').textContent = metricas.contratosPorVencer ?? 0;
    }
}

function renderUltimasFacturas(facturas) {
    const tbody = document.getElementById('dashboard-facturas-body');
    const ultimas = (facturas || []).slice(0, 5);

    tbody.innerHTML = ultimas.map(f => {
        const estado = (f.estado || '').toLowerCase();
        return `
        <tr>
            <td>${f.idFactura ?? ''}</td>
            <td>${f.razonSocial ?? ''}</td>
            <td>RD$${formatearMonto(f.total ?? 0)}</td>
            <td><span class="status-badge status-${estado}">${f.estado}</span></td>
        </tr>`;
    }).join('');
}

function renderChart(facturas) {
    const container = document.getElementById('chart-bars');
    container.innerHTML = '';

    const meses = [];
    const now = new Date();
    for (let i = 5; i >= 0; i--) {
        const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
        meses.push({
            label: d.toLocaleDateString('es-ES', { month: 'short' }),
            mes: d.getMonth(),
            anio: d.getFullYear(),
            ingresos: 0,
            gastos: 0
        });
    }

    (facturas || []).forEach(f => {
        const d = new Date(f.fechaEmision);
        if (isNaN(d.getTime())) return;
        const m = meses.find(m => m.mes === d.getMonth() && m.anio === d.getFullYear());
        if (m) {
            if (f.estado === 'EMITIDA') m.ingresos += f.total ?? 0;
            if (f.estado === 'PAGADA') m.gastos += f.total ?? 0;
        }
    });

    const maxValor = Math.max(...meses.map(m => Math.max(m.ingresos, m.gastos)), 1);

    meses.forEach(m => {
        const group = document.createElement('div');
        group.className = 'chart-bar-group';

        const altIng = (m.ingresos / maxValor) * 180;
        const altGast = (m.gastos / maxValor) * 180;

        group.innerHTML = `
            <div class="chart-bar ingresos" style="height:${Math.max(altIng, 4)}px" title="Ingresos: RD$${formatearMonto(m.ingresos)}"></div>
            <div class="chart-bar gastos" style="height:${Math.max(altGast, 4)}px" title="Gastos: RD$${formatearMonto(m.gastos)}"></div>
            <span class="chart-label">${m.label}</span>
        `;
        container.appendChild(group);
    });
}

function formatearMonto(valor) {
    if (typeof valor !== 'number' || isNaN(valor)) valor = 0;
    return valor.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
