function initAlertas() {
    const btn = document.getElementById('btn-alertas');
    if (!btn) return;
    const dropdown = document.getElementById('alertas-dropdown');

    btn.addEventListener('click', (e) => {
        e.stopPropagation();
        dropdown.classList.toggle('hidden');
        if (!dropdown.classList.contains('hidden')) {
            cargarAlertas();
        }
    });

    document.addEventListener('click', () => {
        dropdown.classList.add('hidden');
    });

    dropdown.addEventListener('click', (e) => {
        e.stopPropagation();
    });

    if (hasPermission('ALERTAS.VER')) {
        cargarContadorAlertas();
        alertasInterval = setInterval(cargarContadorAlertas, 60000);
    }
}

async function cargarAlertas() {
    const list = document.getElementById('alertas-list');

    try {
        const alertas = await apiGet('/alertas?dias=7');

        if (!alertas || alertas.length === 0) {
            list.innerHTML = '<div class="alertas-empty">Sin alertas pendientes</div>';
            return;
        }

        list.innerHTML = alertas.map(a => {
            const criticidadClass = a.criticidad === 'Vencido' ? 'criticidad-vencido' : 'criticidad-proximo';
            return `
                <div class="alerta-item ${criticidadClass}">
                    <div class="alerta-header">
                        <span class="alerta-tipo">${a.tipo ?? ''}</span>
                        <span class="alerta-fecha">${formatearFechaSola(a.fechaVencimiento)}</span>
                    </div>
                    <div class="alerta-body">
                        <strong>${a.referencia ?? ''}</strong> - ${a.entidad ?? ''}
                    </div>
                    <div class="alerta-footer">
                        <span>RD$${formatearMonto(a.monto ?? 0)}</span>
                        <span class="alerta-criticidad">${a.criticidad ?? ''}</span>
                    </div>
                </div>
            `;
        }).join('');
    } catch (err) {
        list.innerHTML = '<div class="alertas-empty">Error cargando alertas</div>';
    }
}

async function cargarContadorAlertas() {
    try {
        const data = await apiGet('/alertas/contador');
        const badge = document.getElementById('alertas-count');

        if (data && data.total > 0) {
            badge.textContent = data.total;
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
    } catch (err) {
        // silently ignore
    }
}

function formatearFechaSola(fechaStr) {
    if (!fechaStr) return '';
    const d = new Date(fechaStr + 'T00:00:00');
    if (isNaN(d.getTime())) return fechaStr;
    return d.toLocaleDateString('es-DO');
}
