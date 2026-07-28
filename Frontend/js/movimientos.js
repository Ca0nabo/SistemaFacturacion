function initMovimientos() {
    onView('movimientos', loadMovimientos);
}

async function loadMovimientos() {
    const tbody = document.getElementById('movimientos-body');
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Cargando movimientos...</td></tr>';

    try {
        const movimientos = await apiGet('/movimientos');

        if (!movimientos || movimientos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;color:var(--gray-500)">No hay movimientos registrados.</td></tr>';
            return;
        }

        tbody.innerHTML = movimientos.map(m => {
            const hoy = new Date(new Date().toDateString());
            const venc = new Date(m.fechaVencimiento + 'T00:00:00');
            const estaVencido = !isNaN(venc.getTime()) && venc < hoy;
            const estaPagado = (m.montoPendiente ?? 0) === 0;

            const badgeClass = estaPagado ? 'status-pagada' : estaVencido ? 'status-vencida' : 'status-pendiente';
            const badgeText = estaPagado ? 'Pagado' : estaVencido ? 'Vencido' : 'Pendiente';

            return `
                <tr>
                    <td>${m.idMovimiento ?? ''}</td>
                    <td>#${m.idFactura ?? ''}</td>
                    <td><span class="status-badge ${m.tipo === 'CxC' ? 'status-emitida' : 'status-pendiente'}">${m.tipo ?? ''}</span></td>
                    <td>RD$${formatearMonto(m.montoPendiente ?? 0)}</td>
                    <td>${!isNaN(venc.getTime()) ? venc.toLocaleDateString('es-DO') : ''}</td>
                    <td><span class="status-badge ${badgeClass}">${badgeText}</span></td>
                    <td class="acciones">
                        ${!estaPagado ? `<button class="btn btn-sm btn-success" onclick="pagarMovimiento(${m.idMovimiento})">Pagar</button>` : ''}
                    </td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="7" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

async function pagarMovimiento(id) {
    if (!confirm('¿Marcar este movimiento como pagado?')) return;
    try {
        await apiPut(`/movimientos/${id}/pagar`);
        loadMovimientos();
    } catch (err) {
        alert(`Error: ${err.message}`);
    }
}
