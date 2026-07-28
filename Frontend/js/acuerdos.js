function initAcuerdos() {
    onView('acuerdos-pago', loadAcuerdos);
}

async function loadAcuerdos() {
    const tbody = document.getElementById('acuerdos-body');
    tbody.innerHTML = '<tr><td colspan="6" class="loading">Cargando acuerdos de pago...</td></tr>';

    try {
        const movimientos = await apiGet('/movimientos');
        const acuerdos = (movimientos || []).filter(m => {
            const cuotas = m.totalCuotas ?? m.cantidadCuotas ?? 0;
            return cuotas > 1;
        });

        if (acuerdos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--gray-500)">No hay acuerdos de pago registrados.</td></tr>';
            return;
        }

        tbody.innerHTML = acuerdos.map(m => {
            const cuotas = m.totalCuotas ?? m.cantidadCuotas ?? 1;
            const montoPorCuota = cuotas > 0 ? (m.montoOriginal ?? m.montoPendiente ?? 0) / cuotas : 0;
            const estado = (m.estado || 'pendiente').toLowerCase();

            return `
                <tr>
                    <td>#${m.idFactura ?? ''}</td>
                    <td>${m.entidad ?? m.cliente ?? m.razonSocial ?? ''}</td>
                    <td>RD$${formatearMonto(m.montoOriginal ?? m.montoPendiente ?? 0)}</td>
                    <td>${cuotas}</td>
                    <td>RD$${formatearMonto(montoPorCuota)}</td>
                    <td><span class="status-badge status-${estado}">${m.estado ?? 'Pendiente'}</span></td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}
